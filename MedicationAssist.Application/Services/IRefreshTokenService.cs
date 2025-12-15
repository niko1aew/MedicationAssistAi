namespace MedicationAssist.Application.Services;

/// <summary>
/// Данные refresh токена для Application layer
/// </summary>
public class RefreshTokenInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Интерфейс сервиса для работы с refresh токенами
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Создать и сохранить новый refresh токен для пользователя
    /// </summary>
    Task<RefreshTokenInfo> CreateTokenAsync(Guid userId, string tokenValue, DateTime expiresAt);
    
    /// <summary>
    /// Получить токен по значению
    /// </summary>
    Task<RefreshTokenInfo?> GetByTokenAsync(string token);
    
    /// <summary>
    /// Отозвать токен и создать новый (ротация)
    /// </summary>
    Task<RefreshTokenInfo> RotateTokenAsync(string oldToken, Guid userId, string newTokenValue, DateTime expiresAt);
    
    /// <summary>
    /// Отозвать токен
    /// </summary>
    Task RevokeTokenAsync(string token);
    
    /// <summary>
    /// Отозвать все токены пользователя
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId);
}

