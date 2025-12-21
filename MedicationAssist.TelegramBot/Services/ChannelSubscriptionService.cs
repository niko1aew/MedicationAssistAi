using MedicationAssist.TelegramBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Сервис для проверки подписки пользователей на обязательный канал
/// </summary>
public class ChannelSubscriptionService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramBotSettings _settings;
    private readonly ILogger<ChannelSubscriptionService> _logger;

    public ChannelSubscriptionService(
        ITelegramBotClient botClient,
        IOptions<TelegramBotSettings> settings,
        ILogger<ChannelSubscriptionService> logger)
    {
        _botClient = botClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет, подписан ли пользователь на обязательный канал
    /// </summary>
    /// <param name="telegramUserId">Telegram ID пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true если подписан, false если нет или проверка невозможна</returns>
    public async Task<bool> CheckSubscriptionAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.RequiredChannelUsername))
        {
            _logger.LogError("Required channel username is not configured. Registration is blocked until channel is configured");
            return false; // Если канал не настроен, блокируем регистрацию
        }

        try
        {
            var channelId = $"@{_settings.RequiredChannelUsername}";

            _logger.LogDebug(
                "Checking subscription for user {UserId} in channel {Channel}",
                telegramUserId, channelId);

            var chatMember = await _botClient.GetChatMember(
                chatId: channelId,
                userId: telegramUserId,
                cancellationToken: cancellationToken);

            // Проверяем статус участника
            var isSubscribed = chatMember.Status switch
            {
                ChatMemberStatus.Creator => true,
                ChatMemberStatus.Administrator => true,
                ChatMemberStatus.Member => true,
                ChatMemberStatus.Restricted => chatMember is ChatMemberRestricted restricted && restricted.IsMember,
                ChatMemberStatus.Left => false,
                ChatMemberStatus.Kicked => false,
                _ => false
            };

            _logger.LogInformation(
                "User {UserId} subscription check result: {IsSubscribed} (status: {Status})",
                telegramUserId, isSubscribed, chatMember.Status);

            return isSubscribed;
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("user not found"))
        {
            _logger.LogWarning(
                "User {UserId} not found in channel {Channel}, treating as not subscribed",
                telegramUserId, _settings.RequiredChannelUsername);
            return false;
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("chat not found"))
        {
            _logger.LogError(
                "Channel {Channel} not found. Please check the channel username configuration. Registration is blocked",
                _settings.RequiredChannelUsername);
            return false; // Если канал не найден, блокируем регистрацию
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("bot is not a member"))
        {
            _logger.LogError(
                "Bot is not a member or administrator of channel {Channel}. Please add the bot as an administrator. Registration is blocked",
                _settings.RequiredChannelUsername);
            return false; // Если бот не админ, блокируем регистрацию
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            _logger.LogError(
                "Telegram API error checking subscription for user {UserId} in channel {Channel}: {ErrorCode} - {ErrorMessage}. Registration is blocked",
                telegramUserId, _settings.RequiredChannelUsername, ex.ErrorCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error checking subscription for user {UserId} in channel {Channel}. Registration is blocked",
                telegramUserId, _settings.RequiredChannelUsername);
            return false; // В случае ошибки блокируем регистрацию
        }
    }

    /// <summary>
    /// Получает полный URL канала из настроек
    /// </summary>
    public string GetChannelUrl()
    {
        return !string.IsNullOrWhiteSpace(_settings.RequiredChannelUrl)
            ? _settings.RequiredChannelUrl
            : $"https://t.me/{_settings.RequiredChannelUsername}";
    }

    /// <summary>
    /// Получает username канала (без @)
    /// </summary>
    public string GetChannelUsername()
    {
        return _settings.RequiredChannelUsername;
    }
}
