using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Reminder service for medication intake
/// </summary>
public class ReminderService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderService> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder service started");

        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await CheckAndSendRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reminder service loop");
            }
        }

        _logger.LogInformation("Reminder service stopped");
    }

    /// <summary>
    /// Добавить напоминание
    /// </summary>
    public async Task<ReminderDto?> AddReminderAsync(
        long telegramUserId,
        Guid medicationId,
        TimeOnly time,
        CancellationToken ct = default)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
            return null;

        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
        var result = await reminderService.CreateAsync(
            session.UserId.Value,
            new CreateReminderDto(telegramUserId, medicationId, time),
            ct);

        if (!result.IsSuccess)
            return null;

        var reminder = result.Data!;

        _logger.LogInformation(
            "Created reminder {ReminderId} for user {UserId}: {MedicationName} at {Time}",
            reminder.Id, session.UserId, reminder.MedicationName, time);

        return reminder;
    }

    /// <summary>
    /// Удалить напоминание
    /// </summary>
    public async Task<bool> RemoveReminderAsync(Guid reminderId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
        var result = await reminderService.DeleteAsync(reminderId, ct);
        return result.IsSuccess;
    }

    /// <summary>
    /// Получить напоминания пользователя
    /// </summary>
    public async Task<IEnumerable<ReminderDto>> GetUserRemindersAsync(long telegramUserId, CancellationToken ct = default)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
            return Enumerable.Empty<ReminderDto>();

        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
        var result = await reminderService.GetByUserIdAsync(session.UserId.Value, ct);

        return result.IsSuccess && result.Data != null
            ? result.Data
            : Enumerable.Empty<ReminderDto>();
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        var nowLocal = DateTime.Now;
        var currentTime = TimeOnly.FromDateTime(nowLocal);
        var today = DateOnly.FromDateTime(nowLocal);

        var remindersResult = await reminderService.GetActiveAsync(ct);
        if (!remindersResult.IsSuccess || remindersResult.Data == null)
            return;

        foreach (var reminder in remindersResult.Data)
        {
            var timeDiff = Math.Abs((currentTime.ToTimeSpan() - reminder.Time.ToTimeSpan()).TotalMinutes);
            
            if (timeDiff > 1)
                continue;

            if (reminder.LastSentAt.HasValue)
            {
                var lastSentDate = DateOnly.FromDateTime(reminder.LastSentAt.Value.ToLocalTime());
                if (lastSentDate == today)
                    continue;
            }

            await SendReminderNotificationAsync(reminder, reminderService, ct);
        }
    }

    private async Task SendReminderNotificationAsync(ReminderDto reminder, IReminderService reminderService, CancellationToken ct)
    {
        try
        {
            var dosageText = string.IsNullOrEmpty(reminder.Dosage) 
                ? Messages.NotSpecified 
                : reminder.Dosage;

            var message = string.Format(
                Messages.ReminderNotification,
                reminder.MedicationName,
                dosageText);

            await _botClient.SendMessage(
                reminder.TelegramUserId,
                message,
                cancellationToken: ct);

            await reminderService.MarkSentAsync(reminder.Id, DateTime.UtcNow, ct);

            _logger.LogInformation(
                "Sent reminder {ReminderId} to user {UserId}",
                reminder.Id, reminder.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending reminder {ReminderId} to user {UserId}",
                reminder.Id, reminder.UserId);
        }
    }

    /// <summary>
    /// Парсинг времени из строки
    /// </summary>
    public static bool TryParseTime(string input, out TimeOnly time)
    {
        time = default;
        
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Пробуем разные форматы
        var formats = new[] { "HH:mm", "H:mm", "HH.mm", "H.mm" };
        
        foreach (var format in formats)
        {
            if (TimeOnly.TryParseExact(input.Trim(), format, out time))
                return true;
        }

        return false;
    }
}

