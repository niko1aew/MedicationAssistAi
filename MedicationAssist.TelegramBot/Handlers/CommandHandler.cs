using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.TelegramBot.Configuration;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Microsoft.Extensions.Options;
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
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly TelegramBotSettings _settings;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        AuthHandler authHandler,
        MedicationHandler medicationHandler,
        IntakeHandler intakeHandler,
        ReminderHandler reminderHandler,
        SettingsHandler settingsHandler,
        IUserService userService,
        IUserRepository userRepository,
        IOptions<TelegramBotSettings> settings,
        ILogger<CommandHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _authHandler = authHandler;
        _medicationHandler = medicationHandler;
        _intakeHandler = intakeHandler;
        _reminderHandler = reminderHandler;
        _settingsHandler = settingsHandler;
        _userService = userService;
        _userRepository = userRepository;
        _settings = settings.Value;
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
        var messageParts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = messageParts[0].ToLower();

        // Убираем @botname если есть
        if (command.Contains('@'))
        {
            command = command.Split('@')[0];
        }

        _logger.LogInformation("Received command {Command} from user {UserId}. Full text: {Text}. Parts count: {Count}",
            command, userId, message.Text, messageParts.Length);

        // Проверяем блокировку пользователя (кроме /start)
        if (command != "/start")
        {
            var user = await _userRepository.GetByTelegramIdAsync(userId, ct);
            if (user?.IsBlocked == true)
            {
                _logger.LogWarning("Blocked user {UserId} tried to execute command {Command}", userId, command);
                await _botClient.SendMessage(
                    chatId,
                    Messages.AccountBlocked.Replace("{reason}", user.BlockedReason ?? "Unknown"),
                    cancellationToken: ct);
                return;
            }
        }

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
                // Проверяем наличие параметра deep link
                var startParameter = messageParts.Length > 1 ? messageParts[1] : null;
                await HandleStartAsync(chatId, userId, message.From, startParameter, ct);
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

    private async Task HandleStartAsync(long chatId, long userId, User telegramUser, string? parameter, CancellationToken ct)
    {
        _logger.LogInformation("HandleStartAsync called with parameter: {Parameter}", parameter);

        // Обработка deep link для привязки Telegram
        if (!string.IsNullOrEmpty(parameter) && parameter.StartsWith("link_"))
        {
            var token = parameter.Substring(5); // Убираем префикс "link_"
            _logger.LogInformation("Detected link token, redirecting to HandleLinkByTokenAsync with token: {Token}", token);
            await _authHandler.HandleLinkByTokenAsync(chatId, telegramUser, token, ct);
            return;
        }

        // Обработка deep link для веб-логина
        if (!string.IsNullOrEmpty(parameter) && parameter.StartsWith("weblogin_"))
        {
            var token = parameter.Substring(9); // Убираем префикс "weblogin_"
            _logger.LogInformation("Detected web login token, redirecting to HandleWebLoginAuthorizationAsync with token: {Token}", token);
            await _authHandler.HandleWebLoginAuthorizationAsync(chatId, telegramUser, token, ct);
            return;
        }

        var session = _sessionService.GetOrCreateSession(userId);

        if (session.IsAuthenticated)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.WelcomeBack, session.UserName),
                replyMarkup: InlineKeyboards.GetMainMenu(_settings.WebsiteUrl),
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
                replyMarkup: InlineKeyboards.GetMainMenu(_settings.WebsiteUrl),
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
            // Пытаемся автоматически авторизовать по Telegram ID
            var autoAuthResult = await TryAutoAuthenticateAsync(userId, ct);

            if (autoAuthResult)
            {
                _logger.LogInformation("User {TelegramUserId} was auto-authenticated", userId);
                return true;
            }

            await _authHandler.ShowAuthMenuAsync(chatId, ct);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Попытаться автоматически авторизовать пользователя по Telegram ID
    /// </summary>
    private async Task<bool> TryAutoAuthenticateAsync(long telegramUserId, CancellationToken ct)
    {
        try
        {
            // Проверяем, есть ли пользователь с таким Telegram ID в базе
            var userResult = await _userService.GetByTelegramIdAsync(telegramUserId, ct);

            if (userResult.IsSuccess && userResult.Data != null)
            {
                // Пользователь найден - авторизуем его в сессии
                _sessionService.Authenticate(telegramUserId, userResult.Data.Id, userResult.Data.Name);

                _logger.LogInformation(
                    "Auto-authenticated Telegram user {TelegramUserId} as {UserName} (ID: {UserId})",
                    telegramUserId, userResult.Data.Name, userResult.Data.Id);

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-authentication for Telegram user {TelegramUserId}", telegramUserId);
        }

        return false;
    }
}

