using System.Collections.Concurrent;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Информация о pending напоминании
/// </summary>
public class PendingReminderInfo
{
    public Guid ReminderId { get; set; }
    public long TelegramUserId { get; set; }
    public Guid UserId { get; set; }
    public Guid MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime FirstSentAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public int MessageId { get; set; } // Последнее отправленное сообщение
}

/// <summary>
/// Трекер pending напоминаний (ожидающих действия пользователя)
/// </summary>
public class PendingReminderTracker
{
    private readonly ConcurrentDictionary<Guid, PendingReminderInfo> _pendingReminders = new();

    /// <summary>
    /// Добавить или обновить pending напоминание
    /// </summary>
    public void AddOrUpdate(PendingReminderInfo info)
    {
        _pendingReminders.AddOrUpdate(info.ReminderId, info, (_, _) => info);
    }

    /// <summary>
    /// Получить pending напоминание
    /// </summary>
    public PendingReminderInfo? Get(Guid reminderId)
    {
        _pendingReminders.TryGetValue(reminderId, out var info);
        return info;
    }

    /// <summary>
    /// Удалить pending напоминание (когда пользователь ответил)
    /// </summary>
    public bool Remove(Guid reminderId)
    {
        return _pendingReminders.TryRemove(reminderId, out _);
    }

    /// <summary>
    /// Получить все pending напоминания, которые нужно повторно отправить
    /// </summary>
    public IEnumerable<PendingReminderInfo> GetRemindersToResend(TimeSpan interval)
    {
        var now = DateTime.UtcNow;
        return _pendingReminders.Values
            .Where(r => now - r.LastSentAt >= interval)
            .ToList();
    }

    /// <summary>
    /// Получить все pending напоминания
    /// </summary>
    public IEnumerable<PendingReminderInfo> GetAll()
    {
        return _pendingReminders.Values.ToList();
    }
}
