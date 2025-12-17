using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик настроек пользователя
/// </summary>
public class SettingsHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly IUserService _userService;
    private readonly ILogger<SettingsHandler> _logger;

    // Популярные часовые пояса для России и СНГ
    private static readonly Dictionary<string, string> CommonTimeZones = new()
    {
        { "Europe/Moscow", "🇷🇺 Москва (UTC+3)" },
        { "Europe/Samara", "🇷🇺 Самара (UTC+4)" },
        { "Asia/Yekaterinburg", "🇷🇺 Екатеринбург (UTC+5)" },
        { "Asia/Omsk", "🇷🇺 Омск (UTC+6)" },
        { "Asia/Krasnoyarsk", "🇷🇺 Красноярск (UTC+7)" },
        { "Asia/Irkutsk", "🇷🇺 Иркутск (UTC+8)" },
        { "Asia/Yakutsk", "🇷🇺 Якутск (UTC+9)" },
        { "Asia/Vladivostok", "🇷🇺 Владивосток (UTC+10)" },
        { "Asia/Magadan", "🇷🇺 Магадан (UTC+11)" },
        { "Asia/Kamchatka", "🇷🇺 Камчатка (UTC+12)" },
        { "Europe/Minsk", "🇧🇾 Минск (UTC+3)" },
        { "Europe/Kiev", "🇺🇦 Киев (UTC+2)" },
        { "Asia/Almaty", "🇰🇿 Алматы (UTC+6)" },
        { "Asia/Tashkent", "🇺🇿 Ташкент (UTC+5)" },
    };

    public SettingsHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IUserService userService,
        ILogger<SettingsHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Показать меню настроек
    /// </summary>
    public async Task ShowSettingsAsync(long chatId, long telegramUserId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var userResult = await _userService.GetByIdAsync(session.UserId.Value, ct);
        if (!userResult.IsSuccess || userResult.Data == null)
        {
            await _botClient.SendMessage(chatId, Messages.UnknownError, cancellationToken: ct);
            return;
        }

        var user = userResult.Data;

        // Получаем текущее время пользователя
        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
        var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

        var settingsText = string.Format(Messages.Settings,
            string.Format(Messages.CurrentTimeZone,
                CommonTimeZones.GetValueOrDefault(user.TimeZoneId, user.TimeZoneId),
                userLocalTime.ToString("HH:mm")));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🌍 Изменить часовой пояс", "settings_timezone") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Главное меню", "menu") }
        });

        await _botClient.SendMessage(
            chatId,
            settingsText,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать меню выбора часового пояса
    /// </summary>
    public async Task ShowTimeZoneSelectorAsync(long chatId, CancellationToken ct)
    {
        var buttons = CommonTimeZones
            .Select(tz => new[]
            {
                InlineKeyboardButton.WithCallbackData(tz.Value, $"timezone:{tz.Key}")
            })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "settings") });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendMessage(
            chatId,
            Messages.SelectTimeZone,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    /// <summary>
    /// Установить часовой пояс пользователя
    /// </summary>
    public async Task SetTimeZoneAsync(long chatId, long telegramUserId, string timeZoneId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var result = await _userService.SetTimeZoneAsync(session.UserId.Value, timeZoneId, ct);

        if (result.IsSuccess)
        {
            var timeZoneName = CommonTimeZones.GetValueOrDefault(timeZoneId, timeZoneId);
            var message = string.Format(Messages.TimeZoneUpdated, timeZoneName);

            await _botClient.SendMessage(chatId, message, cancellationToken: ct);

            _logger.LogInformation(
                "User {TelegramUserId} changed timezone to {TimeZoneId}",
                telegramUserId, timeZoneId);

            await ShowSettingsAsync(chatId, telegramUserId, ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.InvalidTimeZone,
                cancellationToken: ct);
        }
    }
}
