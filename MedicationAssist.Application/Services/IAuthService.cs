using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Интерфейс сервиса аутентификации
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    
    /// <summary>
    /// Вход в систему
    /// </summary>
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto);
    
    /// <summary>
    /// Обновить access токен используя refresh токен
    /// </summary>
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Отозвать refresh токен (logout с текущего устройства)
    /// </summary>
    Task<Result> RevokeTokenAsync(string refreshToken);
    
    /// <summary>
    /// Отозвать все refresh токены пользователя (logout со всех устройств)
    /// </summary>
    Task<Result> RevokeAllTokensAsync(Guid userId);
}

