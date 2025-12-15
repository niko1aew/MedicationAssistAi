using System.ComponentModel.DataAnnotations;

namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO для запроса обновления токена
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh токен
    /// </summary>
    [Required(ErrorMessage = "Refresh токен обязателен")]
    public string RefreshToken { get; set; } = string.Empty;
}

