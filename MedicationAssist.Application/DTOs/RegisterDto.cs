namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO для регистрации нового пользователя
/// </summary>
public class RegisterDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

