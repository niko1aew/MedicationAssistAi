using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Интерфейс для работы с JWT токенами
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Генерирует JWT токен для пользователя
    /// </summary>
    string GenerateToken(User user);
}

