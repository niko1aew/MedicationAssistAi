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
            throw new DomainException("Medication name cannot be empty");

        if (name.Length > 200)
            throw new DomainException("Medication name cannot exceed 200 characters");

        Name = name;
        MarkAsUpdated();
    }

    public void SetDescription(string? description)
    {
        if (description?.Length > 1000)
            throw new DomainException("Medication description cannot exceed 1000 characters");

        Description = description;
        MarkAsUpdated();
    }

    public void SetDosage(string? dosage)
    {
        if (dosage?.Length > 100)
            throw new DomainException("Dosage cannot exceed 100 characters");

        Dosage = dosage;
        MarkAsUpdated();
    }
}

