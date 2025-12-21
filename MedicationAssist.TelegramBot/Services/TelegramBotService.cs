using MedicationAssist.Domain.Repositories;
using MedicationAssist.TelegramBot.Configuration;
using MedicationAssist.TelegramBot.Handlers;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MedicationAssist.Application.Services;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Основной сервис Telegram бота
/// </summary>
public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserSessionService _sessionService;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        UserSessionService sessionService,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _sessionService = sessionService;
        _logger = logger;
    }

    private async Task SetupBotCommandsAsync(CancellationToken ct)
    {
        try
        {
            var commands = new[]
            {
                new BotCommand { Command = "start", Description = "Начать работу с ботом" },
                new BotCommand { Command = "menu", Description = "Главное меню" },
                new BotCommand { Command = "medications", Description = "Список лекарств" },
                new BotCommand { Command = "add", Description = "Добавить лекарство" },
                new BotCommand { Command = "intake", Description = "Записать приём" },
                new BotCommand { Command = "history", Description = "История приёмов" },
                new BotCommand { Command = "reminders", Description = "Управление напоминаниями" },
                new BotCommand { Command = "help", Description = "Показать справку" }
            };

            await _botClient.SetMyCommands(commands, cancellationToken: ct);
            _logger.LogInformation("Bot commands menu configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bot commands menu");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Bot @{BotUsername} (ID: {BotId}) started", me.Username, me.Id);

        // Настройка меню команд
        await SetupBotCommandsAsync(stoppingToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery,
                UpdateType.ChatMember
            },
            DropPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        // Периодическая очистка неактивных сессий
        var cleanupTimer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await cleanupTimer.WaitForNextTickAsync(stoppingToken);
                _sessionService.CleanupInactiveSessions(TimeSpan.FromHours(24));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Bot stopped");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            switch (update.Type)
            {
                case UpdateType.Message:
                    await HandleMessageAsync(scope.ServiceProvider, update.Message!, ct);
                    break;

                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(scope.ServiceProvider, update.CallbackQuery!, ct);
                    break;

                case UpdateType.ChatMember:
                    await HandleChatMemberAsync(scope.ServiceProvider, update.ChatMember!, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
        }
    }

    private async Task HandleMessageAsync(IServiceProvider services, Message message, CancellationToken ct)
    {
        if (message.From == null)
            return;

        var userId = message.From.Id;
        var chatId = message.Chat.Id;
        var text = message.Text;

        _logger.LogDebug(
            "Received message from {UserId} in chat {ChatId}: {Text}",
            userId, chatId, text?.Length > 50 ? text[..50] + "..." : text);

        // Обновляем время последней активности
        _sessionService.GetOrCreateSession(userId).LastActivity = DateTime.UtcNow;

        if (string.IsNullOrEmpty(text))
            return;

        var commandHandler = services.GetRequiredService<CommandHandler>();

        // Проверяем, является ли сообщение командой
        if (text.StartsWith('/'))
        {
            await commandHandler.HandleCommandAsync(message, ct);
        }
        else
        {
            await commandHandler.HandleTextInputAsync(message, ct);
        }
    }

    private async Task HandleCallbackQueryAsync(IServiceProvider services, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (callbackQuery.From == null)
            return;

        var userId = callbackQuery.From.Id;

        _logger.LogDebug(
            "Received callback from {UserId}: {Data}",
            userId, callbackQuery.Data);

        // Обновляем время последней активности
        _sessionService.GetOrCreateSession(userId).LastActivity = DateTime.UtcNow;

        var callbackHandler = services.GetRequiredService<CallbackQueryHandler>();
        await callbackHandler.HandleCallbackQueryAsync(callbackQuery, ct);
    }

    private async Task HandleChatMemberAsync(IServiceProvider services, ChatMemberUpdated chatMemberUpdated, CancellationToken ct)
    {
        try
        {
            var settings = services.GetRequiredService<IOptions<TelegramBotSettings>>().Value;

            // Проверяем, что это событие из нужного нам канала
            if (chatMemberUpdated.Chat.Username?.Equals(settings.RequiredChannelUsername, StringComparison.OrdinalIgnoreCase) != true)
            {
                _logger.LogDebug("Ignoring ChatMember update from channel @{Channel}", chatMemberUpdated.Chat.Username);
                return;
            }

            var userId = chatMemberUpdated.From.Id;
            var oldStatus = chatMemberUpdated.OldChatMember.Status;
            var newStatus = chatMemberUpdated.NewChatMember.Status;

            _logger.LogInformation(
                "User {UserId} changed status in channel @{Channel}: {OldStatus} -> {NewStatus}",
                userId, settings.RequiredChannelUsername, oldStatus, newStatus);

            // Проверяем, отписался ли пользователь
            var wasSubscribed = oldStatus is ChatMemberStatus.Member or ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
            var isSubscribed = newStatus is ChatMemberStatus.Member or ChatMemberStatus.Administrator or ChatMemberStatus.Creator;

            if (wasSubscribed && !isSubscribed)
            {
                // Пользователь отписался - блокируем его
                _logger.LogWarning("User {UserId} unsubscribed from channel, blocking account", userId);

                var userRepository = services.GetRequiredService<IUserRepository>();
                var refreshTokenService = services.GetRequiredService<IRefreshTokenService>();
                var unitOfWork = services.GetRequiredService<IUnitOfWork>();

                var user = await userRepository.GetByTelegramIdAsync(userId, ct);
                if (user != null)
                {
                    user.Block($"Требуется подписка на канал {settings.RequiredChannelUrl}");
                    await unitOfWork.SaveChangesAsync(ct);

                    // Отзываем все refresh токены
                    await refreshTokenService.RevokeAllUserTokensAsync(user.Id);

                    // Отправляем уведомление пользователю
                    try
                    {
                        var channelSubscriptionService = services.GetRequiredService<ChannelSubscriptionService>();
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("📢 Перейти на канал", channelSubscriptionService.GetChannelUrl())
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✅ Я подписался", "recheck_subscription")
                            }
                        });

                        await _botClient.SendMessage(
                            chatId: userId,
                            text: Messages.SubscriptionLostWarning,
                            replyMarkup: keyboard,
                            cancellationToken: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send unsubscribe notification to user {UserId}", userId);
                    }
                }
            }
            else if (!wasSubscribed && isSubscribed)
            {
                // Пользователь подписался - обновляем статус
                _logger.LogInformation("User {UserId} subscribed to channel", userId);

                var userRepository = services.GetRequiredService<IUserRepository>();
                var unitOfWork = services.GetRequiredService<IUnitOfWork>();

                var user = await userRepository.GetByTelegramIdAsync(userId, ct);
                if (user != null)
                {
                    user.UpdateSubscriptionCheck(isSubscribed: true);
                    await unitOfWork.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChatMember update");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram Bot API error");
        return Task.CompletedTask;
    }
}

