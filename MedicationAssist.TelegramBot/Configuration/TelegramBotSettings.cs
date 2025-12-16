namespace MedicationAssist.TelegramBot.Configuration;

/// <summary>
/// Настройки Telegram бота
/// </summary>
/// <remarks>
/// Переменные окружения:
/// - TELEGRAM_BOT_TOKEN - токен бота (обязательно)
/// - TELEGRAM_WEBHOOK_URL - URL для webhook (опционально)
/// - DATABASE_CONNECTION_STRING - строка подключения к БД
/// - JWT_SECRET_KEY - секретный ключ JWT
/// </remarks>
public class TelegramBotSettings
{
    public const string SectionName = "TelegramBot";
    
    /// <summary>
    /// Токен бота, полученный от @BotFather
    /// </summary>
    /// <remarks>
    /// Переменная окружения: TELEGRAM_BOT_TOKEN
    /// </remarks>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// URL для webhook (опционально, если null - используется long polling)
    /// </summary>
    /// <remarks>
    /// Переменная окружения: TELEGRAM_WEBHOOK_URL
    /// </remarks>
    public string? WebhookUrl { get; set; }
}

