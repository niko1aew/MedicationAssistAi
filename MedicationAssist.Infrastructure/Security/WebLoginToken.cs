namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Одноразовый токен для автоматического входа на веб-сайт из Telegram бота (Infrastructure - технический механизм)
/// </summary>
public class WebLoginToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private WebLoginToken()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public WebLoginToken(string token, Guid userId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        Token = token ?? throw new ArgumentNullException(nameof(token));
        UserId = userId;
        ExpiresAt = expiresAt;
        IsUsed = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверить, действителен ли токен
    /// </summary>
    public bool IsValid()
    {
        return !IsUsed && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Пометить токен как использованный
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Token has already been used");

        if (DateTime.UtcNow >= ExpiresAt)
            throw new InvalidOperationException("Token has expired");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
