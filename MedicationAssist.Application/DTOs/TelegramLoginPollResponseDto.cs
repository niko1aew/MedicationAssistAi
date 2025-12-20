namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO ответа на polling статуса авторизации через Telegram
/// </summary>
public class TelegramLoginPollResponseDto
{
    /// <summary>
    /// Статус авторизации: "pending", "authorized", "expired"
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// JWT токен доступа (только при status = "authorized")
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Refresh токен (только при status = "authorized")
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Время истечения токена доступа (только при status = "authorized")
    /// </summary>
    public DateTime? TokenExpires { get; set; }

    /// <summary>
    /// Информация о пользователе (только при status = "authorized")
    /// </summary>
    public UserDto? User { get; set; }
}
