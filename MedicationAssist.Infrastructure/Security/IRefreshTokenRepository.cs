namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Репозиторий для работы с refresh токенами
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Получить токен по значению
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token);
    
    /// <summary>
    /// Получить все активные токены пользователя
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Добавить новый refresh токен
    /// </summary>
    Task AddAsync(RefreshToken refreshToken);
    
    /// <summary>
    /// Обновить токен
    /// </summary>
    Task UpdateAsync(RefreshToken refreshToken);
    
    /// <summary>
    /// Отозвать все токены пользователя (logout со всех устройств)
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId);
}

