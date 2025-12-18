namespace MedicationAssist.Application.Services;

/// <summary>
/// Сервис для работы с токенами привязки Telegram (абстракция в Application Layer)
/// </summary>
public interface ILinkTokenService
{
    /// <summary>
    /// Генерировать новый токен привязки для пользователя
    /// </summary>
    Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Валидировать токен и получить ID пользователя
    /// </summary>
    Task<Guid?> ValidateAndConsumeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить активный токен пользователя (если существует)
    /// </summary>
    Task<string?> GetActiveTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить истекшие токены
    /// </summary>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}
