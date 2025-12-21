using MedicationAssist.Domain.Common;

namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Пользователь системы
/// </summary>
public class User : Entity
{
    private readonly List<Medication> _medications = new();
    private readonly List<MedicationIntake> _medicationIntakes = new();
    private readonly List<Reminder> _reminders = new();

    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public long? TelegramUserId { get; private set; }
    public string? TelegramUsername { get; private set; }
    public string TimeZoneId { get; private set; }

    // Channel subscription tracking
    public bool IsBlocked { get; private set; }
    public string? BlockedReason { get; private set; }
    public DateTime? BlockedAt { get; private set; }
    public DateTime? LastSubscriptionCheckAt { get; private set; }
    public DateTime? ChannelMembershipVerifiedAt { get; private set; }

    // Onboarding tracking
    public bool IsOnboardingCompleted { get; private set; }
    public OnboardingStep? OnboardingStep { get; private set; }

    public IReadOnlyCollection<Medication> Medications => _medications.AsReadOnly();
    public IReadOnlyCollection<MedicationIntake> MedicationIntakes => _medicationIntakes.AsReadOnly();
    public IReadOnlyCollection<Reminder> Reminders => _reminders.AsReadOnly();

    private User() : base()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = UserRole.User;
        TimeZoneId = "Europe/Moscow"; // Default timezone for Russia (UTC+3)
        IsOnboardingCompleted = false;
        OnboardingStep = null;
    }

    public User(string name, string email, string passwordHash, UserRole role = UserRole.User, string? timeZoneId = null) : base()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        TimeZoneId = string.Empty;

        SetName(name);
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = role;
        SetTimeZone(timeZoneId ?? "Europe/Moscow");
        IsOnboardingCompleted = false;
        OnboardingStep = null;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("User name cannot be empty");

        if (name.Length > 200)
            throw new DomainException("User name cannot exceed 200 characters");

        Name = name;
        MarkAsUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        if (!email.Contains('@'))
            throw new DomainException("Invalid email format");

        if (email.Length > 200)
            throw new DomainException("Email cannot exceed 200 characters");

        Email = email;
        MarkAsUpdated();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty");

        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    public void SetRole(UserRole role)
    {
        Role = role;
        MarkAsUpdated();
    }

    public void SetTelegramAccount(long telegramUserId, string? telegramUsername = null)
    {
        if (telegramUserId <= 0)
            throw new DomainException("Telegram User ID must be a positive number");

        TelegramUserId = telegramUserId;
        TelegramUsername = telegramUsername;
        MarkAsUpdated();
    }

    public void RemoveTelegramAccount()
    {
        TelegramUserId = null;
        TelegramUsername = null;
        MarkAsUpdated();
    }

    public void SetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new DomainException("Time zone ID cannot be empty");

        // Validate timezone ID
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException($"Invalid time zone ID: {timeZoneId}");
        }

        TimeZoneId = timeZoneId;
        MarkAsUpdated();
    }

    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Block reason cannot be empty");

        IsBlocked = true;
        BlockedReason = reason;
        BlockedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Unblock()
    {
        IsBlocked = false;
        BlockedReason = null;
        BlockedAt = null;
        MarkAsUpdated();
    }

    public void UpdateSubscriptionCheck(bool isSubscribed)
    {
        LastSubscriptionCheckAt = DateTime.UtcNow;

        if (isSubscribed)
        {
            ChannelMembershipVerifiedAt = DateTime.UtcNow;
            if (IsBlocked)
            {
                Unblock();
            }
        }

        MarkAsUpdated();
    }

    public void UpdateOnboarding(bool? isCompleted = null, OnboardingStep? step = null)
    {
        if (step.HasValue)
        {
            var stepValue = (int)step.Value;
            if (stepValue < 0 || stepValue > 4)
                throw new DomainException("Step must be between 0 and 4");

            OnboardingStep = step.Value;
        }

        if (isCompleted.HasValue)
        {
            IsOnboardingCompleted = isCompleted.Value;
        }

        MarkAsUpdated();
    }

    public Medication AddMedication(string name, string? description = null, string? dosage = null)
    {
        if (_medications.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Medication with name '{name}' already exists in the list");

        var medication = new Medication(Id, name, description, dosage);
        _medications.Add(medication);
        MarkAsUpdated();

        return medication;
    }

    public void RemoveMedication(Guid medicationId)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication == null)
            throw new DomainException("Medication not found");

        // Проверяем, нет ли связанных приемов
        if (_medicationIntakes.Any(mi => mi.MedicationId == medicationId))
            throw new DomainException("Cannot delete medication as there are intake records associated with it");

        _medications.Remove(medication);
        MarkAsUpdated();
    }

    public MedicationIntake RecordMedicationIntake(Guid medicationId, DateTime? intakeTime = null, string? notes = null)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication == null)
            throw new DomainException("Medication not found in user's list");

        var intake = new MedicationIntake(Id, medicationId, intakeTime ?? DateTime.UtcNow, notes);
        _medicationIntakes.Add(intake);
        MarkAsUpdated();

        return intake;
    }

    public void RemoveMedicationIntake(Guid intakeId)
    {
        var intake = _medicationIntakes.FirstOrDefault(mi => mi.Id == intakeId);
        if (intake == null)
            throw new DomainException("Medication intake record not found");

        _medicationIntakes.Remove(intake);
        MarkAsUpdated();
    }

    internal void AddMedicationInternal(Medication medication)
    {
        if (medication.UserId != Id)
            throw new DomainException("Medication belongs to another user");

        _medications.Add(medication);
    }

    internal void AddMedicationIntakeInternal(MedicationIntake intake)
    {
        if (intake.UserId != Id)
            throw new DomainException("Medication intake record belongs to another user");

        _medicationIntakes.Add(intake);
    }

    internal void AddReminderInternal(Reminder reminder)
    {
        if (reminder.UserId != Id)
            throw new DomainException("Reminder belongs to another user");

        _reminders.Add(reminder);
    }
}

