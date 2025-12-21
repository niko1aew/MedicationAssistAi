namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO для авторизации через Telegram Mini App
/// </summary>
public class TelegramWebAppAuthRequest
{
    /// <summary>
    /// InitData строка от Telegram Web App (URL-encoded)
    /// Содержит данные пользователя и подпись для верификации
    /// </summary>
    public string InitData { get; set; } = string.Empty;
}

/// <summary>
/// Данные пользователя из Telegram Web App initData
/// </summary>
public class TelegramWebAppUserData
{
    /// <summary>
    /// Telegram ID пользователя
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Username (@username)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Код языка (например, "ru", "en")
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Является ли пользователь premium
    /// </summary>
    public bool? IsPremium { get; set; }

    /// <summary>
    /// URL фотографии профиля
    /// </summary>
    public string? PhotoUrl { get; set; }
}

/// <summary>
/// Результат валидации initData от Telegram Web App
/// </summary>
public class TelegramWebAppValidationResult
{
    /// <summary>
    /// Успешна ли валидация
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Ошибка валидации (если есть)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Данные пользователя (если валидация успешна)
    /// </summary>
    public TelegramWebAppUserData? User { get; set; }

    /// <summary>
    /// Время авторизации (Unix timestamp)
    /// </summary>
    public long? AuthDate { get; set; }

    public static TelegramWebAppValidationResult Success(TelegramWebAppUserData user, long authDate) =>
        new() { IsValid = true, User = user, AuthDate = authDate };

    public static TelegramWebAppValidationResult Failure(string error) =>
        new() { IsValid = false, Error = error };
}
