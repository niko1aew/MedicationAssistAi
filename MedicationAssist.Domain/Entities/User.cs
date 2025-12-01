using MedicationAssist.Domain.Common;

namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Пользователь системы
/// </summary>
public class User : Entity
{
    private readonly List<Medication> _medications = new();
    private readonly List<MedicationIntake> _medicationIntakes = new();

    public string Name { get; private set; }
    public string Email { get; private set; }

    public IReadOnlyCollection<Medication> Medications => _medications.AsReadOnly();
    public IReadOnlyCollection<MedicationIntake> MedicationIntakes => _medicationIntakes.AsReadOnly();

    private User() : base()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public User(string name, string email) : base()
    {
        SetName(name);
        SetEmail(email);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Имя пользователя не может быть пустым");

        if (name.Length > 200)
            throw new DomainException("Имя пользователя не может превышать 200 символов");

        Name = name;
        MarkAsUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email не может быть пустым");

        if (!email.Contains('@'))
            throw new DomainException("Некорректный формат email");

        if (email.Length > 200)
            throw new DomainException("Email не может превышать 200 символов");

        Email = email;
        MarkAsUpdated();
    }

    public Medication AddMedication(string name, string? description = null, string? dosage = null)
    {
        if (_medications.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Лекарство с названием '{name}' уже существует в списке");

        var medication = new Medication(Id, name, description, dosage);
        _medications.Add(medication);
        MarkAsUpdated();

        return medication;
    }

    public void RemoveMedication(Guid medicationId)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication == null)
            throw new DomainException("Лекарство не найдено");

        // Проверяем, нет ли связанных приемов
        if (_medicationIntakes.Any(mi => mi.MedicationId == medicationId))
            throw new DomainException("Невозможно удалить лекарство, так как существуют записи о его приеме");

        _medications.Remove(medication);
        MarkAsUpdated();
    }

    public MedicationIntake RecordMedicationIntake(Guid medicationId, DateTime? intakeTime = null, string? notes = null)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication == null)
            throw new DomainException("Лекарство не найдено в списке пользователя");

        var intake = new MedicationIntake(Id, medicationId, intakeTime ?? DateTime.UtcNow, notes);
        _medicationIntakes.Add(intake);
        MarkAsUpdated();

        return intake;
    }

    public void RemoveMedicationIntake(Guid intakeId)
    {
        var intake = _medicationIntakes.FirstOrDefault(mi => mi.Id == intakeId);
        if (intake == null)
            throw new DomainException("Запись о приеме лекарства не найдена");

        _medicationIntakes.Remove(intake);
        MarkAsUpdated();
    }

    internal void AddMedicationInternal(Medication medication)
    {
        if (medication.UserId != Id)
            throw new DomainException("Лекарство принадлежит другому пользователю");

        _medications.Add(medication);
    }

    internal void AddMedicationIntakeInternal(MedicationIntake intake)
    {
        if (intake.UserId != Id)
            throw new DomainException("Запись о приеме принадлежит другому пользователю");

        _medicationIntakes.Add(intake);
    }
}

