using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Common;
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
/// Обработчик callback-запросов от inline-клавиатур
/// </summary>
public class CallbackQueryHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly AuthHandler _authHandler;
    private readonly MedicationHandler _medicationHandler;
    private readonly IntakeHandler _intakeHandler;
    private readonly ReminderHandler _reminderHandler;
    private readonly SettingsHandler _settingsHandler;
    private readonly IWebLoginTokenService _webLoginTokenService;
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly ChannelSubscriptionService _channelSubscriptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TelegramBotSettings _settings;
    private readonly ILogger<CallbackQueryHandler> _logger;

    public CallbackQueryHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        AuthHandler authHandler,
        MedicationHandler medicationHandler,
        IntakeHandler intakeHandler,
        ReminderHandler reminderHandler,
        SettingsHandler settingsHandler,
        IWebLoginTokenService webLoginTokenService,
        IUserService userService,
        IUserRepository userRepository,
        ChannelSubscriptionService channelSubscriptionService,
        IUnitOfWork unitOfWork,
        IOptions<TelegramBotSettings> settings,
        ILogger<CallbackQueryHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _authHandler = authHandler;
        _medicationHandler = medicationHandler;
        _intakeHandler = intakeHandler;
        _reminderHandler = reminderHandler;
        _settingsHandler = settingsHandler;
        _webLoginTokenService = webLoginTokenService;
        _userService = userService;
        _userRepository = userRepository;
        _channelSubscriptionService = channelSubscriptionService;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Обработать callback query
    /// </summary>
    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (callbackQuery.Message == null || callbackQuery.From == null || string.IsNullOrEmpty(callbackQuery.Data))
            return;

        var chatId = callbackQuery.Message.Chat.Id;
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data;

        _logger.LogDebug("Received callback {Data} from user {UserId}", data, userId);

        // Отвечаем на callback, чтобы убрать "часики" с кнопки
        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

        // Парсим callback data (может быть в формате "action" или "action:param")
        var parts = data.Split(':');
        var action = parts[0];

        // Проверяем блокировку пользователя (кроме quick_start и recheck_subscription)
        if (action != "quick_start" && action != "recheck_subscription")
        {
            var user = await _userRepository.GetByTelegramIdAsync(userId, ct);
            if (user?.IsBlocked == true)
            {
                _logger.LogWarning("Blocked user {UserId} tried to execute callback {Action}", userId, action);
                await _botClient.SendMessage(
                    chatId,
                    Messages.AccountBlocked.Replace("{reason}", user.BlockedReason ?? "Unknown"),
                    cancellationToken: ct);
                return;
            }
        }
        var param = parts.Length > 1 ? parts[1] : null;

        try
        {
            switch (action)
            {
                // Навигация
                case "main_menu":
                    await HandleMainMenuAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "cancel":
                    await HandleCancelAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                // Аутентификация
                case "quick_start":
                    await _authHandler.QuickStartAsync(chatId, callbackQuery.From, callbackQuery.Message.MessageId, ct);
                    break;

                case "recheck_subscription":
                    await HandleRecheckSubscriptionAsync(chatId, userId, callbackQuery.Message.MessageId, callbackQuery.From, ct);
                    break;

                case "logout":
                    await _authHandler.LogoutAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                // Лекарства
                case "medications":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _medicationHandler.ShowMedicationsMenuAsync(chatId, callbackQuery.Message.MessageId, ct);
                    break;

                case "list_medications":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _medicationHandler.ShowMedicationsListAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "add_medication":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _medicationHandler.StartAddMedicationAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "delete_medication_menu":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _medicationHandler.ShowDeleteMedicationMenuAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "med_details":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var medId))
                    {
                        await _medicationHandler.ShowMedicationDetailsAsync(chatId, medId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                case "confirm_delete_med":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var deleteMedId))
                    {
                        await _medicationHandler.ShowDeleteConfirmationAsync(chatId, deleteMedId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                case "delete_med":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var confirmDeleteMedId))
                    {
                        await _medicationHandler.DeleteMedicationAsync(chatId, confirmDeleteMedId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                // Приёмы
                case "intake":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _intakeHandler.ShowIntakeMenuAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "record_intake":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var intakeMedId))
                    {
                        // Быстрая запись без примечаний
                        await _intakeHandler.QuickRecordIntakeAsync(chatId, userId, intakeMedId, ct);
                    }
                    break;

                case "quick_intake":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var quickIntakeMedId))
                    {
                        await _intakeHandler.QuickRecordIntakeAsync(chatId, userId, quickIntakeMedId, ct);
                    }
                    break;

                // История
                case "history":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (!string.IsNullOrEmpty(param))
                    {
                        await _intakeHandler.ShowIntakeHistoryAsync(chatId, userId, param, callbackQuery.Message.MessageId, ct);
                    }
                    else
                    {
                        await _intakeHandler.ShowHistoryPeriodMenuAsync(chatId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                case "medication_history":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var historyMedId))
                    {
                        await _intakeHandler.ShowMedicationIntakeHistoryAsync(chatId, userId, historyMedId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                // Напоминания
                case "reminders":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _reminderHandler.ShowRemindersMenuAsync(chatId, callbackQuery.Message.MessageId, ct);
                    break;

                case "add_reminder":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _reminderHandler.StartAddReminderAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "list_reminders":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _reminderHandler.ShowRemindersListAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "delete_reminder_menu":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _reminderHandler.ShowDeleteReminderMenuAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "reminder_med":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var reminderMedId))
                    {
                        await _reminderHandler.HandleMedicationSelectedAsync(chatId, userId, reminderMedId, ct);
                    }
                    break;

                case "delete_reminder":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var deleteReminderId))
                    {
                        await _reminderHandler.DeleteReminderAsync(chatId, userId, deleteReminderId, ct);
                    }
                    break;

                // Настройки
                case "settings":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _settingsHandler.ShowSettingsAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "open_website":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await HandleOpenWebsiteAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "settings_timezone":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await _settingsHandler.ShowTimeZoneSelectorAsync(chatId, callbackQuery.Message.MessageId, ct);
                    break;

                case "timezone":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (!string.IsNullOrEmpty(param))
                    {
                        await _settingsHandler.SetTimeZoneAsync(chatId, userId, param, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                case "med_add_reminder":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var medReminderId))
                    {
                        await _reminderHandler.AddReminderForMedicationAsync(chatId, userId, medReminderId, ct);
                    }
                    break;

                // Действия с напоминаниями о приёме
                case "take_reminder":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    await HandleTakeReminderAsync(chatId, userId, parts, callbackQuery.Message.MessageId, ct);
                    break;

                case "skip_reminder":
                    if (!await EnsureAuthenticatedAsync(chatId, userId, callbackQuery.Message.MessageId, ct)) return;
                    if (Guid.TryParse(param, out var skipReminderId))
                    {
                        await HandleSkipReminderAsync(chatId, skipReminderId, callbackQuery.Message.MessageId, ct);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown callback: {Data}", data);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing callback {Data}", data);
            await _botClient.SendMessage(
                chatId,
                Messages.UnknownError,
                cancellationToken: ct);
        }
    }

    private async Task HandleMainMenuAsync(long chatId, long userId, int messageId, CancellationToken ct)
    {
        _sessionService.ResetState(userId);
        var session = _sessionService.GetSession(userId);

        if (session?.IsAuthenticated == true)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                Messages.MainMenu,
                replyMarkup: InlineKeyboards.GetMainMenu(_settings.WebsiteUrl),
                cancellationToken: ct);
        }
        else
        {
            await _authHandler.ShowAuthMenuAsync(chatId, messageId, ct);
        }
    }

    private async Task HandleCancelAsync(long chatId, long userId, int messageId, CancellationToken ct)
    {
        _sessionService.ResetState(userId);

        await _botClient.EditMessageText(
            chatId,
            messageId,
            Messages.OperationCancelled,
            cancellationToken: ct);

        // Показать главное меню через небольшую задержку
        await Task.Delay(500, ct);
        await HandleMainMenuAsync(chatId, userId, messageId, ct);
    }

    private async Task HandleSettingsAsync(long chatId, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId,
            "⚙️ Настройки",
            replyMarkup: InlineKeyboards.SettingsMenu,
            cancellationToken: ct);
    }

    private async Task HandleTakeReminderAsync(long chatId, long userId, string[] parts, int messageId, CancellationToken ct)
    {
        // Формат: take_reminder:reminderId
        if (parts.Length < 2 ||
            !Guid.TryParse(parts[1], out var reminderId))
        {
            _logger.LogWarning("Invalid take_reminder callback format: {Parts}", string.Join(":", parts));
            return;
        }

        // Проверяем, не обрабатывается ли уже это напоминание
        if (!_reminderHandler.TryStartProcessingReminder(reminderId))
        {
            _logger.LogDebug("Reminder {ReminderId} is already being processed, ignoring duplicate request", reminderId);
            return; // Напоминание уже обрабатывается, игнорируем дубликат
        }

        var medicationName = await _reminderHandler.GetMedicationNameAsync(reminderId, ct);

        try
        {
            // Сразу обновляем интерфейс, показывая пользователю статус обработки
            await _botClient.EditMessageText(
                chatId,
                messageId,
                string.Format("⏳ Записываю приём {0}...", medicationName ?? "лекарства"),
                cancellationToken: ct);

            var success = await _reminderHandler.HandleReminderTakenAsync(reminderId, userId, ct);

            if (success)
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId,
                    string.Format(Messages.ReminderTaken, medicationName ?? "лекарство"),
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId,
                    Messages.UnknownError,
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling take reminder {ReminderId}", reminderId);

            try
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId,
                    Messages.UnknownError,
                    cancellationToken: ct);
            }
            catch (Exception editEx)
            {
                _logger.LogError(editEx, "Error updating message after failed reminder handling");
            }

            // Снимаем флаг обработки в случае ошибки
            _reminderHandler.ClearProcessingReminder(reminderId);
        }
    }

    private async Task HandleSkipReminderAsync(long chatId, Guid reminderId, int messageId, CancellationToken ct)
    {
        // Проверяем, не обрабатывается ли уже это напоминание
        if (!_reminderHandler.TryStartProcessingReminder(reminderId))
        {
            _logger.LogDebug("Reminder {ReminderId} is already being processed, ignoring duplicate skip request", reminderId);
            return; // Напоминание уже обрабатывается, игнорируем дубликат
        }

        try
        {
            var medicationName = await _reminderHandler.GetMedicationNameAsync(reminderId, ct);
            await _reminderHandler.HandleReminderSkippedAsync(reminderId, ct);

            await _botClient.EditMessageText(
                chatId,
                messageId,
                string.Format(Messages.ReminderSkipped, medicationName ?? "лекарство"),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling skip reminder {ReminderId}", reminderId);
            _reminderHandler.ClearProcessingReminder(reminderId);
            throw;
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync(long chatId, long userId, int messageId, CancellationToken ct)
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

            await _authHandler.ShowAuthMenuAsync(chatId, messageId, ct);
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

    /// <summary>
    /// Обработать повторную проверку подписки на канал
    /// </summary>
    private async Task HandleRecheckSubscriptionAsync(long chatId, long userId, int messageId, User telegramUser, CancellationToken ct)
    {
        try
        {
            // Проверяем подписку
            var isSubscribed = await _channelSubscriptionService.CheckSubscriptionAsync(userId, ct);

            if (!isSubscribed)
            {
                _logger.LogInformation("Subscription check failed for Telegram user {TelegramUserId}, still not subscribed", userId);

                var message = Messages.SubscriptionCheckFailed
                    .Replace("{channelUrl}", _settings.RequiredChannelUrl ?? "")
                    .Replace("{channelName}", _settings.RequiredChannelUsername ?? "");

                await _botClient.EditMessageText(
                    chatId,
                    messageId,
                    message,
                    replyMarkup: SubscriptionKeyboard.GetKeyboard(_settings.RequiredChannelUrl ?? ""),
                    cancellationToken: ct);
                return;
            }

            // Пользователь подписан - проверяем, есть ли он в базе
            var user = await _userRepository.GetByTelegramIdAsync(userId, ct);

            if (user == null)
            {
                // Пользователь не найден - запускаем регистрацию (он ещё не был создан)
                _logger.LogInformation("User with Telegram ID {TelegramId} not found, starting registration after subscription confirmed", userId);
                await _authHandler.QuickStartAsync(chatId, telegramUser, messageId, ct);
                return;
            }

            // Пользователь найден и был заблокирован - разблокируем
            if (user.IsBlocked)
            {
                user.Unblock();
                await _userRepository.UpdateAsync(user, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("User {UserId} subscription verified, account unblocked", user.Id);
            }

            // Авторизуем в сессии
            _sessionService.Authenticate(userId, user.Id, user.Name);

            await _botClient.EditMessageText(
                chatId,
                messageId,
                Messages.SubscriptionCheckSuccess,
                replyMarkup: InlineKeyboards.GetMainMenu(_settings.WebsiteUrl),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while rechecking subscription for user {UserId}", userId);
            await _botClient.SendMessage(
                chatId,
                Messages.ErrorOccurred,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Обработать открытие веб-сайта
    /// </summary>
    private async Task HandleOpenWebsiteAsync(long chatId, long userId, int messageId, CancellationToken ct)
    {
        try
        {
            var session = _sessionService.GetSession(userId);
            if (session?.UserId == null)
            {
                _logger.LogWarning("Attempting to open website for unauthenticated user {UserId}", userId);
                return;
            }

            // Генерируем токен веб-логина
            var token = await _webLoginTokenService.GenerateTokenAsync(session.UserId.Value, ct);
            var url = $"{_settings.WebsiteUrl}/auth/telegram?token={token}";

            // Отправляем сообщение с URL кнопкой
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                new[]
                {
                    new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("🌐 Открыть сайт", url) },
                    new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("◀️ Главное меню", "main_menu") }
                });

            await _botClient.EditMessageText(
                chatId,
                messageId,
                "🌐 Нажмите кнопку для входа на сайт\n\n⏱ Ссылка действительна <b>5 минут</b>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);

            _logger.LogInformation("Generated web login token for user {UserId}", session.UserId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling open website for user {UserId}", userId);
            await _botClient.SendMessage(
                chatId,
                "❌ Произошла ошибка при генерации ссылки. Попробуйте позже.",
                cancellationToken: ct);
        }
    }
}

