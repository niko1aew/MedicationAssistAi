using System.ComponentModel.DataAnnotations;

namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO для отзыва токена
/// </summary>
public class RevokeTokenRequestDto
{
    /// <summary>
    /// Refresh токен для отзыва
    /// </summary>
    [Required(ErrorMessage = "Refresh токен обязателен")]
    public string RefreshToken { get; set; } = string.Empty;
}

