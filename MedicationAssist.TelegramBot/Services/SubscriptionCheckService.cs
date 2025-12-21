using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.TelegramBot.Configuration;
using MedicationAssist.TelegramBot.Resources;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Фоновый сервис для ежедневной проверки подписки пользователей на обязательный канал
/// </summary>
public class SubscriptionCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramBotSettings _settings;
    private readonly ILogger<SubscriptionCheckService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public SubscriptionCheckService(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient botClient,
        IOptions<TelegramBotSettings> settings,
        ILogger<SubscriptionCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _botClient = botClient;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionCheckService started");

        // Задержка перед первой проверкой (10 минут после старта)
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

        var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllUsersSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subscription check cycle");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SubscriptionCheckService stopped");
    }

    private async Task CheckAllUsersSubscriptionsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.RequiredChannelUsername))
        {
            _logger.LogDebug("Channel subscription check is disabled (no channel configured)");
            return;
        }

        _logger.LogInformation("Starting subscription check for all users");

        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        var channelSubscriptionService = scope.ServiceProvider.GetRequiredService<ChannelSubscriptionService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Получаем всех незаблокированных пользователей с Telegram ID
        var users = await userRepository.GetAllAsync(cancellationToken);
        var telegramUsers = users.Where(u => u.TelegramUserId.HasValue && !u.IsBlocked).ToList();

        _logger.LogInformation("Checking subscriptions for {Count} users", telegramUsers.Count);

        var blockedCount = 0;

        foreach (var user in telegramUsers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var isSubscribed = await channelSubscriptionService.CheckSubscriptionAsync(
                    user.TelegramUserId!.Value,
                    cancellationToken);

                user.UpdateSubscriptionCheck(isSubscribed);

                if (!isSubscribed)
                {
                    // Блокируем пользователя
                    user.Block($"Требуется подписка на канал {channelSubscriptionService.GetChannelUrl()}");
                    blockedCount++;

                    _logger.LogWarning(
                        "User {UserId} (Telegram: {TelegramUserId}) blocked due to missing subscription",
                        user.Id, user.TelegramUserId);

                    // Отзываем все refresh токены
                    await refreshTokenService.RevokeAllUserTokensAsync(user.Id);

                    // Отправляем уведомление пользователю
                    try
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("📢 Подписаться на канал", channelSubscriptionService.GetChannelUrl())
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✅ Я подписался", "recheck_subscription")
                            }
                        });

                        await _botClient.SendMessage(
                            chatId: user.TelegramUserId.Value,
                            text: Messages.SubscriptionLostWarning,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send subscription warning to user {UserId} (Telegram: {TelegramUserId})",
                            user.Id, user.TelegramUserId);
                    }
                }

                // Небольшая задержка между проверками, чтобы не перегружать API
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking subscription for user {UserId} (Telegram: {TelegramUserId})",
                    user.Id, user.TelegramUserId);
            }
        }

        // Сохраняем изменения
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription check completed. Checked: {Total}, Blocked: {Blocked}",
            telegramUsers.Count, blockedCount);
    }
}
