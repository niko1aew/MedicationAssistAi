namespace MedicationAssist.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с токенами веб-логина через Telegram
/// </summary>
public interface ITelegramLoginTokenRepository
{
    /// <summary>
    /// Создать новый токен авторизации
    /// </summary>
    Task<string> CreateTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Установить токен как авторизованный
    /// </summary>
    Task SetAuthorizedAsync(string token, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Получить информацию об авторизации по токену
    /// </summary>
    Task<TelegramLoginTokenInfo?> GetTokenInfoAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Удалить истекшие токены
    /// </summary>
    Task DeleteExpiredAsync(CancellationToken ct = default);
}

/// <summary>
/// Информация о токене авторизации через Telegram
/// </summary>
public class TelegramLoginTokenInfo
{
    public bool IsAuthorized { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
