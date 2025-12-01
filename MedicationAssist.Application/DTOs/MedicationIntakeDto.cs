namespace MedicationAssist.Application.DTOs;

public record MedicationIntakeDto(
    Guid Id,
    Guid UserId,
    Guid MedicationId,
    string MedicationName,
    DateTime IntakeTime,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateMedicationIntakeDto(
    Guid MedicationId,
    DateTime? IntakeTime,
    string? Notes
);

public record UpdateMedicationIntakeDto(
    DateTime IntakeTime,
    string? Notes
);

public record MedicationIntakeFilterDto(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? MedicationId
);

