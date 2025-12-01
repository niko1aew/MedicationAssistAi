using MedicationAssist.Domain.Common;

namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Запись о приеме лекарства
/// </summary>
public class MedicationIntake : Entity
{
    public Guid UserId { get; private set; }
    public Guid MedicationId { get; private set; }
    public DateTime IntakeTime { get; private set; }
    public string? Notes { get; private set; }

    private MedicationIntake() : base()
    {
    }

    public MedicationIntake(Guid userId, Guid medicationId, DateTime intakeTime, string? notes = null) : base()
    {
        UserId = userId;
        MedicationId = medicationId;
        SetIntakeTime(intakeTime);
        SetNotes(notes);
    }

    public void SetIntakeTime(DateTime intakeTime)
    {
        if (intakeTime > DateTime.UtcNow.AddDays(1))
            throw new DomainException("Время приема не может быть более чем через день");

        IntakeTime = intakeTime;
        MarkAsUpdated();
    }

    public void SetNotes(string? notes)
    {
        if (notes?.Length > 500)
            throw new DomainException("Примечания не могут превышать 500 символов");

        Notes = notes;
        MarkAsUpdated();
    }
}

