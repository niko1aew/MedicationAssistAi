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
}

