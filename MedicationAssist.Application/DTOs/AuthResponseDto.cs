namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO ответа при успешной аутентификации
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

