using MedicationAssist.Domain.Common;

namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Лекарство
/// </summary>
public class Medication : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Dosage { get; private set; }

    private Medication() : base()
    {
        Name = string.Empty;
    }

    public Medication(Guid userId, string name, string? description = null, string? dosage = null) : base()
    {
        UserId = userId;
        SetName(name);
        SetDescription(description);
        SetDosage(dosage);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название лекарства не может быть пустым");

        if (name.Length > 200)
            throw new DomainException("Название лекарства не может превышать 200 символов");

        Name = name;
        MarkAsUpdated();
    }

    public void SetDescription(string? description)
    {
        if (description?.Length > 1000)
            throw new DomainException("Описание лекарства не может превышать 1000 символов");

        Description = description;
        MarkAsUpdated();
    }

    public void SetDosage(string? dosage)
    {
        if (dosage?.Length > 100)
            throw new DomainException("Дозировка не может превышать 100 символов");

        Dosage = dosage;
        MarkAsUpdated();
    }
}

