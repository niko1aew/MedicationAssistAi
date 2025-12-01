namespace MedicationAssist.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateUserDto(
    string Name,
    string Email
);

public record UpdateUserDto(
    string Name,
    string Email
);

