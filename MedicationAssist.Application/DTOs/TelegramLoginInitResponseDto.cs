namespace MedicationAssist.Application.DTOs;

/// <summary>
/// DTO ответа на инициализацию входа через Telegram
/// </summary>
public class TelegramLoginInitResponseDto
{
    /// <summary>
    /// Токен для авторизации
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Deep link для перехода в Telegram бот
    /// </summary>
    public required string DeepLink { get; set; }

    /// <summary>
    /// Время действия токена в минутах
    /// </summary>
    public int ExpiresInMinutes { get; set; }

    /// <summary>
    /// URL для polling статуса авторизации
    /// </summary>
    public required string PollUrl { get; set; }
}
