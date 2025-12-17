using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик текстовых команд бота
/// </summary>
public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly AuthHandler _authHandler;
    private readonly MedicationHandler _medicationHandler;
    private readonly IntakeHandler _intakeHandler;
    private readonly ReminderHandler _reminderHandler;
    private readonly SettingsHandler _settingsHandler;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        AuthHandler authHandler,
        MedicationHandler medicationHandler,
        IntakeHandler intakeHandler,
        ReminderHandler reminderHandler,
        SettingsHandler settingsHandler,
        ILogger<CommandHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _authHandler = authHandler;
        _medicationHandler = medicationHandler;
        _intakeHandler = intakeHandler;
        _reminderHandler = reminderHandler;
        _settingsHandler = settingsHandler;
        _logger = logger;
    }

    /// <summary>
    /// Обработать команду
    /// </summary>
    public async Task HandleCommandAsync(Message message, CancellationToken ct)
    {
        if (message.From == null || string.IsNullOrEmpty(message.Text))
            return;

        var chatId = message.Chat.Id;
        var userId = message.From.Id;
        var command = message.Text.Split(' ')[0].ToLower();

        // Убираем @botname если есть
        if (command.Contains('@'))
        {
            command = command.Split('@')[0];
        }

        _logger.LogDebug("Received command {Command} from user {UserId}", command, userId);

        var session = _sessionService.GetOrCreateSession(userId);

        // Команда /cancel всегда доступна
        if (command == "/cancel")
        {
            await HandleCancelAsync(chatId, userId, ct);
            return;
        }

        // Команда /skip для пропуска опциональных полей
        if (command == "/skip")
        {
            await HandleSkipAsync(chatId, userId, message.Text, ct);
            return;
        }

        switch (command)
        {
            case "/start":
                await HandleStartAsync(chatId, userId, message.From, ct);
                break;

            case "/help":
                await HandleHelpAsync(chatId, ct);
                break;

            case "/menu":
                await HandleMenuAsync(chatId, userId, ct);
                break;

            case "/medications":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _medicationHandler.ShowMedicationsListAsync(chatId, userId, ct);
                break;

            case "/add":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _medicationHandler.StartAddMedicationAsync(chatId, userId, ct);
                break;

            case "/intake":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _intakeHandler.ShowIntakeMenuAsync(chatId, userId, ct);
                break;

            case "/history":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _intakeHandler.ShowHistoryPeriodMenuAsync(chatId, ct);
                break;

            case "/reminders":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _reminderHandler.ShowRemindersMenuAsync(chatId, ct);
                break;

            case "/settings":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _settingsHandler.ShowSettingsAsync(chatId, userId, ct);
                break;

            case "/logout":
                if (!await EnsureAuthenticatedAsync(chatId, userId, ct)) return;
                await _authHandler.LogoutAsync(chatId, userId, ct);
                break;

            default:
                // Неизвестная команда
                if (command.StartsWith("/"))
                {
                    await _botClient.SendMessage(
                        chatId,
                        Messages.UnknownCommand,
                        cancellationToken: ct);
                }
                break;
        }
    }

    /// <summary>
    /// Обработать текстовый ввод (не команду)
    /// </summary>
    public async Task HandleTextInputAsync(Message message, CancellationToken ct)
    {
        if (message.From == null || string.IsNullOrEmpty(message.Text))
            return;

        var chatId = message.Chat.Id;
        var userId = message.From.Id;
        var text = message.Text.Trim();

        var session = _sessionService.GetOrCreateSession(userId);

        // Обрабатываем в зависимости от состояния диалога
        switch (session.State)
        {
            // Аутентификация - вход
            case ConversationState.AwaitingEmail:
                await _authHandler.HandleEmailInputAsync(chatId, userId, text, ct);
                break;

            case ConversationState.AwaitingPassword:
                await _authHandler.HandlePasswordInputAsync(chatId, message.From, text, ct);
                break;

            // Аутентификация - регистрация
            case ConversationState.AwaitingRegisterName:
                await _authHandler.HandleRegisterNameInputAsync(chatId, userId, text, ct);
                break;

            case ConversationState.AwaitingRegisterEmail:
                await _authHandler.HandleRegisterEmailInputAsync(chatId, userId, text, ct);
                break;

            case ConversationState.AwaitingRegisterPassword:
                await _authHandler.HandleRegisterPasswordInputAsync(chatId, message.From, text, ct);
                break;

            // Лекарства
            case ConversationState.AwaitingMedicationName:
                await _medicationHandler.HandleMedicationNameInputAsync(chatId, userId, text, ct);
                break;

            case ConversationState.AwaitingMedicationDosage:
                await _medicationHandler.HandleMedicationDosageInputAsync(chatId, userId, text, ct);
                break;

            case ConversationState.AwaitingMedicationDescription:
                await _medicationHandler.HandleMedicationDescriptionInputAsync(chatId, userId, text, ct);
                break;

            // Приёмы
            case ConversationState.AwaitingIntakeNotes:
                await _intakeHandler.HandleIntakeNotesInputAsync(chatId, userId, text, ct);
                break;

            // Напоминания
            case ConversationState.AwaitingReminderTime:
                await _reminderHandler.HandleTimeInputAsync(chatId, userId, text, ct);
                break;

            // Нет активного состояния
            case ConversationState.None:
            default:
                // Показываем главное меню или меню аутентификации
                await HandleMenuAsync(chatId, userId, ct);
                break;
        }
    }

    private async Task HandleStartAsync(long chatId, long userId, User telegramUser, CancellationToken ct)
    {
        var session = _sessionService.GetOrCreateSession(userId);

        if (session.IsAuthenticated)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.WelcomeBack, session.UserName),
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.Welcome,
                cancellationToken: ct);

            await _authHandler.ShowAuthMenuAsync(chatId, ct);
        }
    }

    private async Task HandleHelpAsync(long chatId, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId,
            Messages.Help,
            cancellationToken: ct);
    }

    private async Task HandleMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var session = _sessionService.GetOrCreateSession(userId);

        if (session.IsAuthenticated)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.MainMenu,
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);
        }
        else
        {
            await _authHandler.ShowAuthMenuAsync(chatId, ct);
        }
    }

    private async Task HandleCancelAsync(long chatId, long userId, CancellationToken ct)
    {
        _sessionService.ResetState(userId);

        await _botClient.SendMessage(
            chatId,
            Messages.OperationCancelled,
            cancellationToken: ct);

        await HandleMenuAsync(chatId, userId, ct);
    }

    private async Task HandleSkipAsync(long chatId, long userId, string text, CancellationToken ct)
    {
        var session = _sessionService.GetOrCreateSession(userId);

        // Передаём обработку соответствующему хендлеру с null/skip значением
        switch (session.State)
        {
            case ConversationState.AwaitingMedicationDosage:
                await _medicationHandler.HandleMedicationDosageInputAsync(chatId, userId, "/skip", ct);
                break;

            case ConversationState.AwaitingMedicationDescription:
                await _medicationHandler.HandleMedicationDescriptionInputAsync(chatId, userId, "/skip", ct);
                break;

            case ConversationState.AwaitingIntakeNotes:
                await _intakeHandler.HandleIntakeNotesInputAsync(chatId, userId, "/skip", ct);
                break;

            default:
                await _botClient.SendMessage(chatId, Messages.Skipped, cancellationToken: ct);
                break;
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync(long chatId, long userId, CancellationToken ct)
    {
        var session = _sessionService.GetOrCreateSession(userId);

        if (!session.IsAuthenticated)
        {
            await _authHandler.ShowAuthMenuAsync(chatId, ct);
            return false;
        }

        return true;
    }
}

