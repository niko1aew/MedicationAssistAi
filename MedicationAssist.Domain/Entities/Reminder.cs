using MedicationAssist.Domain.Common;

namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Напоминание о приеме лекарства
/// </summary>
public class Reminder : Entity
{
    public Guid UserId { get; private set; }
    public long TelegramUserId { get; private set; }
    public Guid MedicationId { get; private set; }
    public TimeOnly Time { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSentAt { get; private set; }

    private Reminder() : base()
    {
    }

    public Reminder(Guid userId, long telegramUserId, Guid medicationId, TimeOnly time, bool isActive = true) : base()
    {
        SetUser(userId);
        SetTelegramUser(telegramUserId);
        SetMedication(medicationId);
        SetTime(time);
        IsActive = isActive;
    }

    public void SetUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User is not specified");

        UserId = userId;
        MarkAsUpdated();
    }

    public void SetTelegramUser(long telegramUserId)
    {
        if (telegramUserId <= 0)
            throw new DomainException("Telegram user is not specified");

        TelegramUserId = telegramUserId;
        MarkAsUpdated();
    }

    public void SetMedication(Guid medicationId)
    {
        if (medicationId == Guid.Empty)
            throw new DomainException("Medication is not specified");

        MedicationId = medicationId;
        MarkAsUpdated();
    }

    public void SetTime(TimeOnly time)
    {
        Time = time;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        LastSentAt = sentAtUtc;
        MarkAsUpdated();
    }
}

