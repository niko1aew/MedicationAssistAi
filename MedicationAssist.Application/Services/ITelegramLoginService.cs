namespace MedicationAssist.Application.Services;

/// <summary>
/// Сервис управления состоянием авторизации через Telegram для веб-входа
/// </summary>
public interface ITelegramLoginService
{
    /// <summary>
    /// Генерирует анонимный токен для инициализации веб-входа через Telegram
    /// </summary>
    /// <returns>Уникальный токен</returns>
    Task<string> GenerateAnonymousTokenAsync();

    /// <summary>
    /// Отмечает токен как авторизованный указанным пользователем
    /// </summary>
    /// <param name="token">Токен авторизации</param>
    /// <param name="userId">ID пользователя</param>
    Task SetAuthorizedAsync(string token, Guid userId);

    /// <summary>
    /// Проверяет статус авторизации по токену
    /// </summary>
    /// <param name="token">Токен авторизации</param>
    /// <returns>Статус авторизации или null, если токен не найден</returns>
    Task<TelegramLoginStatus?> CheckAuthorizationStatusAsync(string token);
}

/// <summary>
/// Статус авторизации через Telegram
/// </summary>
public class TelegramLoginStatus
{
    /// <summary>
    /// Авторизован ли пользователь
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// ID авторизованного пользователя
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Время создания токена
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Время авторизации
    /// </summary>
    public DateTime? AuthorizedAt { get; set; }
}
