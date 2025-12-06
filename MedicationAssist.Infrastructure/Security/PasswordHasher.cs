using MedicationAssist.Application.Services;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация хэширования паролей с использованием BCrypt
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}

