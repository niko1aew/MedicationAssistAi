namespace MedicationAssist.Application.DTOs;

public record ReminderDto(
    Guid Id,
    Guid UserId,
    long TelegramUserId,
    Guid MedicationId,
    string MedicationName,
    string? Dosage,
    TimeOnly Time,
    bool IsActive,
    DateTime? LastSentAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateReminderDto(
    long TelegramUserId,
    Guid MedicationId,
    TimeOnly Time
);

