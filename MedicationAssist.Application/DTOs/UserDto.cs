using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    long? TelegramUserId,
    string? TelegramUsername,
    string TimeZoneId,
    bool IsOnboardingCompleted,
    OnboardingStep? OnboardingStep,
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

public record LinkTelegramDto(
    long TelegramUserId,
    string? TelegramUsername
);

public record UpdateOnboardingDto(
    bool? IsCompleted,
    int? Step
);
