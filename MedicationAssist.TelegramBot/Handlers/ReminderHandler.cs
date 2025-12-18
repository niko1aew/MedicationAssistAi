using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using BotReminderService = MedicationAssist.TelegramBot.Services.ReminderService;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик команд напоминаний
/// </summary>
public class ReminderHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly BotReminderService _reminderService;
    private readonly IMedicationService _medicationService;
    private readonly ILogger<ReminderHandler> _logger;

    public ReminderHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        BotReminderService reminderService,
        IMedicationService medicationService,
        ILogger<ReminderHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _reminderService = reminderService;
        _medicationService = medicationService;
        _logger = logger;
    }

    /// <summary>
    /// Показать меню напоминаний
    /// </summary>
    public Task ShowRemindersMenuAsync(long chatId, CancellationToken ct)
    {
        return ShowRemindersMenuAsync(chatId, null, ct);
    }

    /// <summary>
    /// Показать меню напоминаний (с возможностью редактирования)
    /// </summary>
    public async Task ShowRemindersMenuAsync(long chatId, int? messageId, CancellationToken ct)
    {
        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId.Value,
                Messages.RemindersMenu,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.RemindersMenu,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Показать список напоминаний
    /// </summary>
    public Task ShowRemindersListAsync(long chatId, long telegramUserId, CancellationToken ct)
    {
        return ShowRemindersListAsync(chatId, telegramUserId, null, ct);
    }

    /// <summary>
    /// Показать список напоминаний (с возможностью редактирования)
    /// </summary>
    public async Task ShowRemindersListAsync(long chatId, long telegramUserId, int? messageId, CancellationToken ct)
    {
        var reminders = (await _reminderService.GetUserRemindersAsync(telegramUserId, ct)).ToList();

        if (!reminders.Any())
        {
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId.Value,
                    Messages.NoReminders,
                    replyMarkup: InlineKeyboards.RemindersMenu,
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    Messages.NoReminders,
                    replyMarkup: InlineKeyboards.RemindersMenu,
                    cancellationToken: ct);
            }
            return;
        }

        var remindersList = string.Join("",
            reminders.Select(r => string.Format(Messages.ReminderItem, r.Time.ToString("HH:mm"), r.MedicationName)));

        var message = string.Format(Messages.RemindersList, remindersList);

        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId.Value,
                message,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                message,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Начать процесс добавления напоминания - показать список лекарств
    /// </summary>
    public Task StartAddReminderAsync(long chatId, long telegramUserId, CancellationToken ct)
    {
        return StartAddReminderAsync(chatId, telegramUserId, null, ct);
    }

    /// <summary>
    /// Начать процесс добавления напоминания (с возможностью редактирования)
    /// </summary>
    public async Task StartAddReminderAsync(long chatId, long telegramUserId, int? messageId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
        {
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId.Value,
                    Messages.AuthRequired,
                    replyMarkup: InlineKeyboards.AuthMenu,
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    Messages.AuthRequired,
                    replyMarkup: InlineKeyboards.AuthMenu,
                    cancellationToken: ct);
            }
            return;
        }

        var result = await _medicationService.GetByUserIdAsync(session.UserId.Value, ct);

        if (!result.IsSuccess || !result.Data!.Any())
        {
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId.Value,
                    Messages.NoMedications,
                    replyMarkup: InlineKeyboards.MedicationsMenu,
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    Messages.NoMedications,
                    replyMarkup: InlineKeyboards.MedicationsMenu,
                    cancellationToken: ct);
            }
            return;
        }

        var buttons = result.Data!
            .Select(m => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"💊 {m.Name}",
                    $"reminder_med:{m.Id}")
            })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "reminders") });

        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId.Value,
                Messages.SelectMedicationForReminder,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.SelectMedicationForReminder,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Выбрано лекарство для напоминания - запросить время
    /// </summary>
    public async Task HandleMedicationSelectedAsync(long chatId, long telegramUserId, Guid medicationId, CancellationToken ct)
    {
        _sessionService.SetTempData(telegramUserId, "reminder_med_id", medicationId.ToString());
        _sessionService.SetState(telegramUserId, ConversationState.AwaitingReminderTime);

        await _botClient.SendMessage(
            chatId,
            Messages.EnterReminderTime,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод времени напоминания
    /// </summary>
    public async Task HandleTimeInputAsync(long chatId, long telegramUserId, string timeInput, CancellationToken ct)
    {
        if (!BotReminderService.TryParseTime(timeInput, out var time))
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Неверный формат времени. Введите время в формате ЧЧ:ММ (например, 08:00 или 14:30):",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        var medIdStr = _sessionService.GetTempData<string>(telegramUserId, "reminder_med_id");
        if (string.IsNullOrEmpty(medIdStr) || !Guid.TryParse(medIdStr, out var medicationId))
        {
            _sessionService.ResetState(telegramUserId);
            await ShowRemindersMenuAsync(chatId, ct);
            return;
        }

        var reminder = await _reminderService.AddReminderAsync(telegramUserId, medicationId, time, ct);

        _sessionService.ResetState(telegramUserId);

        if (reminder != null)
        {
            var message = string.Format(Messages.ReminderSet, reminder.MedicationName, time.ToString("HH:mm"));

            await _botClient.SendMessage(
                chatId,
                message,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "User {TelegramUserId} added reminder for {MedicationName} at {Time}",
                telegramUserId, reminder.MedicationName, time);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.UnknownError,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Показать меню удаления напоминаний
    /// </summary>
    public Task ShowDeleteReminderMenuAsync(long chatId, long telegramUserId, CancellationToken ct)
    {
        return ShowDeleteReminderMenuAsync(chatId, telegramUserId, null, ct);
    }

    /// <summary>
    /// Показать меню удаления напоминаний (с возможностью редактирования)
    /// </summary>
    public async Task ShowDeleteReminderMenuAsync(long chatId, long telegramUserId, int? messageId, CancellationToken ct)
    {
        var reminders = (await _reminderService.GetUserRemindersAsync(telegramUserId, ct)).ToList();

        if (!reminders.Any())
        {
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId.Value,
                    Messages.NoReminders,
                    replyMarkup: InlineKeyboards.RemindersMenu,
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    Messages.NoReminders,
                    replyMarkup: InlineKeyboards.RemindersMenu,
                    cancellationToken: ct);
            }
            return;
        }

        var buttons = reminders
            .Select(r => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🗑️ {r.Time:HH:mm} - {r.MedicationName}",
                    $"delete_reminder:{r.Id}")
            })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "reminders") });

        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId.Value,
                Messages.SelectReminderToDelete,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                Messages.SelectReminderToDelete,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Удалить напоминание
    /// </summary>
    public async Task DeleteReminderAsync(long chatId, long telegramUserId, Guid reminderId, CancellationToken ct)
    {
        var deleted = await _reminderService.RemoveReminderAsync(reminderId, ct);

        if (deleted)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.ReminderDeleted,
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "User {TelegramUserId} deleted reminder {ReminderId}",
                telegramUserId, reminderId);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Напоминание не найдено.",
                replyMarkup: InlineKeyboards.RemindersMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Добавить напоминание для конкретного лекарства (из карточки лекарства)
    /// </summary>
    public async Task AddReminderForMedicationAsync(long chatId, long telegramUserId, Guid medicationId, CancellationToken ct)
    {
        await HandleMedicationSelectedAsync(chatId, telegramUserId, medicationId, ct);
    }
}

