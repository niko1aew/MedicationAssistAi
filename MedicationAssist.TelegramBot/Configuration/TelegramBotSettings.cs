namespace MedicationAssist.TelegramBot.Configuration;

/// <summary>
/// Настройки Telegram бота
/// </summary>
/// <remarks>
/// Переменные окружения:
/// - TELEGRAM_BOT_TOKEN - токен бота (обязательно)
/// - TELEGRAM_WEBHOOK_URL - URL для webhook (опционально)
/// - TELEGRAM_BOT_USERNAME - username бота без @ (обязательно)
/// - WEBSITE_URL - URL веб-сайта проекта (обязательно)
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

    /// <summary>
    /// Username бота (без @) для формирования deep links
    /// </summary>
    /// <remarks>
    /// Переменная окружения: TELEGRAM_BOT_USERNAME
    /// </remarks>
    public string BotUsername { get; set; } = string.Empty;

    /// <summary>
    /// URL веб-сайта проекта для автологина из бота
    /// </summary>
    /// <remarks>
    /// Переменная окружения: WEBSITE_URL
    /// </remarks>
    public string WebsiteUrl { get; set; } = string.Empty;
}

