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
    DateTime? UpdatedAt,
    DateTime? PendingUntil,
    DateTime? PendingFirstSentAt,
    DateTime? PendingLastSentAt,
    int? PendingMessageId
);

public record CreateReminderDto(
    long TelegramUserId,
    Guid MedicationId,
    TimeOnly Time
);

