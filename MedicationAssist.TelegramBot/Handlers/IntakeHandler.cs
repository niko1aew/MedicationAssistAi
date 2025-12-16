using System.Text;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик команд записи приёмов лекарств
/// </summary>
public class IntakeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly IMedicationService _medicationService;
    private readonly IMedicationIntakeService _intakeService;
    private readonly ILogger<IntakeHandler> _logger;

    public IntakeHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IMedicationService medicationService,
        IMedicationIntakeService intakeService,
        ILogger<IntakeHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _medicationService = medicationService;
        _intakeService = intakeService;
        _logger = logger;
    }

    /// <summary>
    /// Показать меню выбора лекарства для приёма
    /// </summary>
    public async Task ShowIntakeMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var result = await _medicationService.GetByUserIdAsync(session.UserId.Value, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
            return;
        }

        var medications = result.Data!.ToList();
        
        if (medications.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.NoMedications,
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
            return;
        }

        await _botClient.SendMessage(
            chatId,
            Messages.SelectMedicationForIntake,
            replyMarkup: InlineKeyboards.MedicationsList(medications, "record_intake"),
            cancellationToken: ct);
    }

    /// <summary>
    /// Быстрая запись приёма (без примечаний)
    /// </summary>
    public async Task QuickRecordIntakeAsync(long chatId, long userId, Guid medicationId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var createDto = new CreateMedicationIntakeDto(medicationId, DateTime.UtcNow, null);
        var result = await _intakeService.CreateAsync(session.UserId.Value, createDto, ct);

        if (result.IsSuccess)
        {
            var intake = result.Data!;
            var timeStr = intake.IntakeTime.ToLocalTime().ToString(Messages.DateTimeFormat);
            
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.IntakeRecorded, intake.MedicationName, timeStr),
                replyMarkup: InlineKeyboards.AfterIntakeMenu,
                cancellationToken: ct);
            
            _logger.LogInformation(
                "User {UserId} recorded medication intake {MedicationId}",
                session.UserId, medicationId);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Начать запись приёма с примечанием
    /// </summary>
    public async Task StartRecordIntakeWithNotesAsync(long chatId, long userId, Guid medicationId, CancellationToken ct)
    {
        _sessionService.SetTempData(userId, "intakeMedicationId", medicationId.ToString());
        _sessionService.SetState(userId, ConversationState.AwaitingIntakeNotes);
        
        await _botClient.SendMessage(
            chatId,
            Messages.EnterIntakeNotes,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод примечания и записать приём
    /// </summary>
    public async Task HandleIntakeNotesInputAsync(long chatId, long userId, string? notes, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            _sessionService.ResetState(userId);
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var medicationIdStr = _sessionService.GetTempData<string>(userId, "intakeMedicationId");
        if (string.IsNullOrEmpty(medicationIdStr) || !Guid.TryParse(medicationIdStr, out var medicationId))
        {
            _sessionService.ResetState(userId);
            await ShowIntakeMenuAsync(chatId, userId, ct);
            return;
        }

        if (notes?.ToLower() == "/skip")
        {
            notes = null;
        }
        else if (notes?.Length > 500)
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Примечание не может превышать 500 символов.",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        var createDto = new CreateMedicationIntakeDto(medicationId, DateTime.UtcNow, notes);
        var result = await _intakeService.CreateAsync(session.UserId.Value, createDto, ct);

        _sessionService.ResetState(userId);

        if (result.IsSuccess)
        {
            var intake = result.Data!;
            var timeStr = intake.IntakeTime.ToLocalTime().ToString(Messages.DateTimeFormat);
            
            var message = string.IsNullOrEmpty(notes)
                ? string.Format(Messages.IntakeRecorded, intake.MedicationName, timeStr)
                : string.Format(Messages.IntakeRecordedWithNotes, intake.MedicationName, timeStr, notes);
            
            await _botClient.SendMessage(
                chatId,
                message,
                replyMarkup: InlineKeyboards.AfterIntakeMenu,
                cancellationToken: ct);
            
            _logger.LogInformation(
                "User {UserId} recorded medication intake {MedicationId} with notes",
                session.UserId, medicationId);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Показать меню выбора периода истории
    /// </summary>
    public async Task ShowHistoryPeriodMenuAsync(long chatId, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId,
            Messages.SelectHistoryPeriod,
            replyMarkup: InlineKeyboards.HistoryPeriodMenu,
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать историю приёмов за период
    /// </summary>
    public async Task ShowIntakeHistoryAsync(long chatId, long userId, string period, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var (fromDate, toDate) = GetDateRangeFromPeriod(period);
        var filter = new MedicationIntakeFilterDto(fromDate, toDate, null);

        var result = await _intakeService.GetByUserIdAsync(session.UserId.Value, filter, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
            return;
        }

        var intakes = result.Data!.ToList();
        
        if (intakes.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.IntakeHistoryEmpty,
                replyMarkup: InlineKeyboards.HistoryPeriodMenu,
                cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder();
        var currentDate = DateTime.MinValue;
        
        foreach (var intake in intakes.OrderByDescending(i => i.IntakeTime))
        {
            var intakeDate = intake.IntakeTime.ToLocalTime().Date;
            
            if (intakeDate != currentDate)
            {
                currentDate = intakeDate;
                var dateStr = GetFriendlyDate(intakeDate);
                sb.AppendLine($"\n📅 {dateStr}:");
            }
            
            var timeStr = intake.IntakeTime.ToLocalTime().ToString(Messages.TimeFormat);
            var notesStr = string.IsNullOrEmpty(intake.Notes) 
                ? "" 
                : string.Format(Messages.IntakeNotes, intake.Notes);
            
            sb.AppendFormat(Messages.IntakeHistoryItem, 
                intake.MedicationName, 
                "",
                timeStr, 
                notesStr);
        }

        var periodName = GetPeriodName(period);
        var headerText = $"📜 История приёмов ({periodName}):\n{sb}";
        
        // Telegram имеет ограничение на длину сообщения
        if (headerText.Length > 4000)
        {
            headerText = headerText[..4000] + "\n\n... (показаны не все записи)";
        }

        await _botClient.SendMessage(
            chatId,
            headerText,
            replyMarkup: InlineKeyboards.HistoryPeriodMenu,
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать историю приёмов конкретного лекарства
    /// </summary>
    public async Task ShowMedicationIntakeHistoryAsync(long chatId, long userId, Guid medicationId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var filter = new MedicationIntakeFilterDto(null, null, medicationId);
        var result = await _intakeService.GetByUserIdAsync(session.UserId.Value, filter, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                cancellationToken: ct);
            return;
        }

        var intakes = result.Data!.Take(20).ToList();
        
        if (intakes.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.IntakeHistoryEmpty,
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
            return;
        }

        var medName = intakes.First().MedicationName;
        var sb = new StringBuilder($"📜 История приёмов \"{medName}\":\n\n");
        
        foreach (var intake in intakes)
        {
            var dateTimeStr = intake.IntakeTime.ToLocalTime().ToString(Messages.DateTimeFormat);
            var notesStr = string.IsNullOrEmpty(intake.Notes) 
                ? "" 
                : $"\n  📝 {intake.Notes}";
            
            sb.AppendLine($"• 🕐 {dateTimeStr}{notesStr}");
        }

        if (result.Data!.Count() > 20)
        {
            sb.AppendLine("\n... (показаны последние 20 записей)");
        }

        await _botClient.SendMessage(
            chatId,
            sb.ToString(),
            replyMarkup: InlineKeyboards.BackToMainMenu,
            cancellationToken: ct);
    }

    private static (DateTime? fromDate, DateTime? toDate) GetDateRangeFromPeriod(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "today" => (now.Date, now.Date.AddDays(1)),
            "yesterday" => (now.Date.AddDays(-1), now.Date),
            "week" => (now.Date.AddDays(-7), null),
            "month" => (now.Date.AddDays(-30), null),
            "all" => (null, null),
            _ => (null, null)
        };
    }

    private static string GetPeriodName(string period)
    {
        return period switch
        {
            "today" => Messages.Today,
            "yesterday" => Messages.Yesterday,
            "week" => Messages.LastWeek,
            "month" => Messages.LastMonth,
            "all" => Messages.AllTime,
            _ => Messages.AllTime
        };
    }

    private static string GetFriendlyDate(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today)
            return Messages.Today;
        if (date == today.AddDays(-1))
            return Messages.Yesterday;
        return date.ToString(Messages.DateFormat);
    }
}

