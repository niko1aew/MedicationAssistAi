using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Keyboards;
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
    private readonly PendingReminderTracker _pendingTracker;

    public ReminderService(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderService> logger,
        PendingReminderTracker pendingTracker)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pendingTracker = pendingTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder service started");

        // Восстанавливаем pending напоминания из БД при запуске
        await RestorePendingRemindersAsync(stoppingToken);

        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await CheckAndSendRemindersAsync(stoppingToken);
                await ResendPendingRemindersAsync(stoppingToken);
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
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var nowUtc = DateTime.UtcNow;

        var remindersResult = await reminderService.GetActiveAsync(ct);
        if (!remindersResult.IsSuccess || remindersResult.Data == null)
            return;

        foreach (var reminder in remindersResult.Data)
        {
            // Получаем пользователя для определения его часового пояса
            var userResult = await userService.GetByIdAsync(reminder.UserId, ct);
            if (!userResult.IsSuccess || userResult.Data == null)
                continue;

            // Конвертируем текущее UTC время в часовой пояс пользователя
            TimeZoneInfo userTimeZone;
            try
            {
                userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(userResult.Data.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("Invalid timezone {TimeZoneId} for user {UserId}, using UTC",
                    userResult.Data.TimeZoneId, reminder.UserId);
                userTimeZone = TimeZoneInfo.Utc;
            }

            var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, userTimeZone);
            var currentTime = TimeOnly.FromDateTime(userLocalTime);
            var today = DateOnly.FromDateTime(userLocalTime);

            // Проверяем, совпадает ли текущее время с временем напоминания (с точностью до 1 минуты)
            var timeDiff = Math.Abs((currentTime.ToTimeSpan() - reminder.Time.ToTimeSpan()).TotalMinutes);

            if (timeDiff > 1)
                continue;

            // Проверяем, не отправляли ли уже сегодня
            if (reminder.LastSentAt.HasValue)
            {
                var lastSentInUserTz = TimeZoneInfo.ConvertTimeFromUtc(reminder.LastSentAt.Value, userTimeZone);
                var lastSentDate = DateOnly.FromDateTime(lastSentInUserTz);
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

            var keyboard = InlineKeyboards.ReminderActions(reminder.Id, reminder.MedicationId);

            var sentMessage = await _botClient.SendMessage(
                reminder.TelegramUserId,
                message,
                replyMarkup: keyboard,
                cancellationToken: ct);

            // Сохраняем pending состояние в БД
            var now = DateTime.UtcNow;
            await reminderService.SetPendingAsync(reminder.Id, now, now, sentMessage.MessageId, ct);
            await reminderService.MarkSentAsync(reminder.Id, now, ct);

            // Также добавляем в in-memory трекер для быстрого доступа
            _pendingTracker.AddOrUpdate(new PendingReminderInfo
            {
                ReminderId = reminder.Id,
                TelegramUserId = reminder.TelegramUserId,
                UserId = reminder.UserId,
                MedicationId = reminder.MedicationId,
                MedicationName = reminder.MedicationName,
                Dosage = dosageText,
                FirstSentAt = now,
                LastSentAt = now,
                MessageId = sentMessage.MessageId
            });

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
    /// Восстановить pending напоминания из БД при запуске сервиса
    /// </summary>
    private async Task RestorePendingRemindersAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

            var pendingResult = await reminderService.GetPendingAsync(ct);
            if (!pendingResult.IsSuccess || pendingResult.Data == null)
                return;

            var pendingReminders = pendingResult.Data.ToList();

            foreach (var reminder in pendingReminders)
            {
                var dosageText = string.IsNullOrEmpty(reminder.Dosage)
                    ? Messages.NotSpecified
                    : reminder.Dosage;

                _pendingTracker.AddOrUpdate(new PendingReminderInfo
                {
                    ReminderId = reminder.Id,
                    TelegramUserId = reminder.TelegramUserId,
                    UserId = reminder.UserId,
                    MedicationId = reminder.MedicationId,
                    MedicationName = reminder.MedicationName,
                    Dosage = dosageText,
                    FirstSentAt = reminder.PendingFirstSentAt ?? DateTime.UtcNow,
                    LastSentAt = reminder.PendingLastSentAt ?? DateTime.UtcNow,
                    MessageId = reminder.PendingMessageId ?? 0
                });
            }

            _logger.LogInformation(
                "Restored {Count} pending reminders from database",
                pendingReminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring pending reminders from database");
        }
    }

    /// <summary>
    /// Повторная отправка pending напоминаний каждые 15 минут
    /// </summary>
    private async Task ResendPendingRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        // Получаем pending напоминания из БД
        var pendingResult = await reminderService.GetPendingAsync(ct);
        if (!pendingResult.IsSuccess || pendingResult.Data == null)
            return;

        var now = DateTime.UtcNow;
        var remindersToResend = pendingResult.Data
            .Where(r => r.PendingLastSentAt.HasValue &&
                       (now - r.PendingLastSentAt.Value) >= TimeSpan.FromMinutes(15))
            .ToList();

        foreach (var reminder in remindersToResend)
        {
            try
            {
                var dosageText = string.IsNullOrEmpty(reminder.Dosage)
                    ? Messages.NotSpecified
                    : reminder.Dosage;

                var message = string.Format(
                    Messages.ReminderNotificationResend,
                    reminder.MedicationName,
                    dosageText);

                var keyboard = InlineKeyboards.ReminderActions(reminder.Id, reminder.MedicationId);
                int? newMessageId = null;

                // Пробуем отредактировать предыдущее сообщение
                if (reminder.PendingMessageId.HasValue)
                {
                    try
                    {
                        await _botClient.EditMessageText(
                            reminder.TelegramUserId,
                            reminder.PendingMessageId.Value,
                            message,
                            replyMarkup: keyboard,
                            cancellationToken: ct);
                    }
                    catch
                    {
                        // Если не удалось отредактировать (например, сообщение удалено), отправляем новое
                        var sentMessage = await _botClient.SendMessage(
                            reminder.TelegramUserId,
                            message,
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        newMessageId = sentMessage.MessageId;
                    }
                }
                else
                {
                    // Нет messageId, отправляем новое сообщение
                    var sentMessage = await _botClient.SendMessage(
                        reminder.TelegramUserId,
                        message,
                        replyMarkup: keyboard,
                        cancellationToken: ct);

                    newMessageId = sentMessage.MessageId;
                }

                // Обновляем время последней отправки в БД
                await reminderService.UpdatePendingSentAsync(reminder.Id, now, newMessageId, ct);

                // Обновляем in-memory трекер
                var pending = _pendingTracker.Get(reminder.Id);
                if (pending != null)
                {
                    pending.LastSentAt = now;
                    if (newMessageId.HasValue)
                        pending.MessageId = newMessageId.Value;
                    _pendingTracker.AddOrUpdate(pending);
                }

                _logger.LogInformation(
                    "Resent pending reminder {ReminderId} to user {UserId}",
                    reminder.Id, reminder.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error resending pending reminder {ReminderId} to user {UserId}",
                    reminder.Id, reminder.UserId);
            }
        }
    }

    /// <summary>
    /// Обработать подтверждение приёма лекарства из напоминания
    /// </summary>
    public async Task<bool> HandleReminderTakenAsync(Guid reminderId, long telegramUserId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(telegramUserId);
        if (session?.UserId == null)
            return false;

        // Получаем информацию о напоминании из трекера
        var pendingInfo = _pendingTracker.Get(reminderId);
        if (pendingInfo == null)
        {
            _logger.LogWarning("Pending reminder info not found for {ReminderId}", reminderId);
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var intakeService = scope.ServiceProvider.GetRequiredService<IMedicationIntakeService>();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        // Записываем приём лекарства с текущим временем
        var result = await intakeService.CreateAsync(
            session.UserId.Value,
            new CreateMedicationIntakeDto(pendingInfo.MedicationId, DateTime.UtcNow, null),
            ct);

        if (result.IsSuccess)
        {
            // Очищаем pending состояние в БД
            await reminderService.ClearPendingAsync(reminderId, ct);

            // Удаляем из in-memory трекера
            _pendingTracker.Remove(reminderId);

            _logger.LogInformation(
                "User {UserId} confirmed medication intake from reminder {ReminderId}",
                session.UserId.Value, reminderId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Обработать пропуск приёма лекарства из напоминания
    /// </summary>
    public async Task HandleReminderSkippedAsync(Guid reminderId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        // Очищаем pending состояние в БД
        await reminderService.ClearPendingAsync(reminderId, ct);

        // Удаляем из in-memory трекера
        _pendingTracker.Remove(reminderId);

        _logger.LogInformation("User skipped reminder {ReminderId}", reminderId);
    }

    /// <summary>
    /// Получить название лекарства из pending напоминания
    /// </summary>
    public string? GetMedicationNameFromPending(Guid reminderId)
    {
        var pending = _pendingTracker.Get(reminderId);
        return pending?.MedicationName;
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

