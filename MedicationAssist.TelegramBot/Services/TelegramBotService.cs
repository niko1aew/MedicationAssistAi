using MedicationAssist.TelegramBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Bot @{BotUsername} (ID: {BotId}) started", me.Username, me.Id);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
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

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram Bot API error");
        return Task.CompletedTask;
    }
}

