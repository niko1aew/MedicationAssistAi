namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Сущность refresh токена для управления сессиями пользователей
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    
    /// <summary>
    /// Токен был отозван
    /// </summary>
    public bool IsRevoked => RevokedAt != null;
    
    /// <summary>
    /// Токен истёк
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    /// <summary>
    /// Токен активен (не отозван и не истёк)
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}

