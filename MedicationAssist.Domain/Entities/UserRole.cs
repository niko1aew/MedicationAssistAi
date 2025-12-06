namespace MedicationAssist.Domain.Entities;

/// <summary>
/// Роли пользователей в системе
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Обычный пользователь
    /// </summary>
    User = 0,
    
    /// <summary>
    /// Администратор системы
    /// </summary>
    Admin = 1
}

