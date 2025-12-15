namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Настройки JWT токенов
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// Время жизни access токена в минутах (по умолчанию 60 минут)
    /// </summary>
    public int ExpirationInMinutes { get; set; } = 60;
    
    /// <summary>
    /// Время жизни refresh токена в днях (по умолчанию 7 дней)
    /// </summary>
    public int RefreshTokenExpirationInDays { get; set; } = 7;
}

