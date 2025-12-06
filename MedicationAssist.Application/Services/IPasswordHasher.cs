namespace MedicationAssist.Application.Services;

/// <summary>
/// Интерфейс для хэширования и проверки паролей
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Хэширует пароль
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Проверяет соответствие пароля его хэшу
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}

