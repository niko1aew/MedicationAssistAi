using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Токен для авторизации через Telegram (веб-логин)
/// Технический объект инфраструктуры, не является доменной сущностью
/// </summary>
public class TelegramLoginToken
{
    /// <summary>
    /// Уникальный токен
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID пользователя, который авторизовался (null, если еще не авторизован)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Авторизован ли токен
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Время создания токена
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Время авторизации
    /// </summary>
    public DateTime? AuthorizedAt { get; set; }

    /// <summary>
    /// Время истечения токена
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Навигационное свойство к пользователю
    /// </summary>
    public User? User { get; set; }
}
