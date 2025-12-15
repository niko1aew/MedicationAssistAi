using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Результат генерации токенов (access + refresh)
/// </summary>
public class TokenGenerationResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpires { get; set; }
    public DateTime RefreshTokenExpires { get; set; }
    public Guid RefreshTokenId { get; set; }
}

/// <summary>
/// Интерфейс для работы с JWT токенами
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Генерирует JWT access токен для пользователя
    /// </summary>
    string GenerateToken(User user);
    
    /// <summary>
    /// Генерирует access и refresh токены для пользователя
    /// </summary>
    TokenGenerationResult GenerateTokens(User user);
    
    /// <summary>
    /// Получает время истечения access токена
    /// </summary>
    DateTime GetAccessTokenExpiration();
    
    /// <summary>
    /// Получает время истечения refresh токена
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}

