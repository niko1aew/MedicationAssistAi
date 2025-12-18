using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
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
    private readonly ILogger<CallbackQueryHandler> _logger;

    public CallbackQueryHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        AuthHandler authHandler,
        MedicationHandler medicationHandler,
        IntakeHandler intakeHandler,
        ReminderHandler reminderHandler,
        SettingsHandler settingsHandler,
        ILogger<CallbackQueryHandler> logger)
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
                case "login":
                    await _authHandler.StartLoginAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "register":
                    await _authHandler.StartRegisterAsync(chatId, userId, callbackQuery.Message.MessageId, ct);
                    break;

                case "quick_start":
                    await _authHandler.QuickStartAsync(chatId, callbackQuery.From, callbackQuery.Message.MessageId, ct);
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
                replyMarkup: InlineKeyboards.MainMenu,
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
        // Формат: take_reminder:reminderId:medicationId
        if (parts.Length < 3 ||
            !Guid.TryParse(parts[1], out var reminderId) ||
            !Guid.TryParse(parts[2], out var medicationId))
        {
            _logger.LogWarning("Invalid take_reminder callback format: {Parts}", string.Join(":", parts));
            return;
        }

        var medicationName = await _reminderHandler.GetMedicationNameAsync(reminderId, ct);
        var success = await _reminderHandler.HandleReminderTakenAsync(reminderId, medicationId, userId, ct);

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
            await _botClient.SendMessage(
                chatId,
                Messages.UnknownError,
                cancellationToken: ct);
        }
    }

    private async Task HandleSkipReminderAsync(long chatId, Guid reminderId, int messageId, CancellationToken ct)
    {
        var medicationName = await _reminderHandler.GetMedicationNameAsync(reminderId, ct);
        await _reminderHandler.HandleReminderSkippedAsync(reminderId, ct);

        await _botClient.EditMessageText(
            chatId,
            messageId,
            string.Format(Messages.ReminderSkipped, medicationName ?? "лекарство"),
            cancellationToken: ct);
    }

    private async Task<bool> EnsureAuthenticatedAsync(long chatId, long userId, int messageId, CancellationToken ct)
    {
        var session = _sessionService.GetOrCreateSession(userId);

        if (!session.IsAuthenticated)
        {
            await _authHandler.ShowAuthMenuAsync(chatId, messageId, ct);
            return false;
        }

        return true;
    }
}

