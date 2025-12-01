namespace MedicationAssist.Application.DTOs;

public record MedicationDto(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string? Dosage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateMedicationDto(
    string Name,
    string? Description,
    string? Dosage
);

public record UpdateMedicationDto(
    string Name,
    string? Description,
    string? Dosage
);

