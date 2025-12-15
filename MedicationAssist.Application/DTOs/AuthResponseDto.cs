namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO ответа при успешной аутентификации
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Access токен (сохраняем для обратной совместимости)
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh токен для обновления access токена
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Время истечения access токена
    /// </summary>
    public DateTime? TokenExpires { get; set; }
    
    /// <summary>
    /// Информация о пользователе
    /// </summary>
    public UserDto User { get; set; } = null!;
}

