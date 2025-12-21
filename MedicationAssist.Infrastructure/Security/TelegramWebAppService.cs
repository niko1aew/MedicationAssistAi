using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Сервис для работы с Telegram Mini App
/// Валидирует initData согласно документации Telegram:
/// https://core.telegram.org/bots/webapps#validating-data-received-via-the-mini-app
/// </summary>
public class TelegramWebAppService : ITelegramWebAppService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramWebAppService> _logger;
    private string? _botToken;

    /// <summary>
    /// Максимальный срок действия данных (1 час)
    /// </summary>
    private const int MaxAuthAgeSeconds = 3600;

    public TelegramWebAppService(
        IConfiguration configuration,
        ILogger<TelegramWebAppService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string BotToken => _botToken ??= _configuration["TelegramBot:Token"]
        ?? throw new InvalidOperationException("TelegramBot:Token not configured. Set TELEGRAM_BOT_TOKEN environment variable.");

    /// <inheritdoc />
    public TelegramWebAppValidationResult ValidateInitData(string initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
        {
            _logger.LogWarning("Empty initData received");
            return TelegramWebAppValidationResult.Failure("InitData is empty");
        }

        try
        {
            // Парсим URL-encoded строку
            var data = HttpUtility.ParseQueryString(initData);

            var hash = data["hash"];
            if (string.IsNullOrEmpty(hash))
            {
                _logger.LogWarning("Hash not found in initData");
                return TelegramWebAppValidationResult.Failure("Hash not found");
            }

            // Проверяем auth_date
            var authDateStr = data["auth_date"];
            if (string.IsNullOrEmpty(authDateStr) || !long.TryParse(authDateStr, out var authDate))
            {
                _logger.LogWarning("Invalid auth_date in initData");
                return TelegramWebAppValidationResult.Failure("Invalid auth_date");
            }

            // Проверяем срок действия
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - authDate > MaxAuthAgeSeconds)
            {
                _logger.LogWarning("InitData expired. AuthDate: {AuthDate}, Now: {Now}", authDate, now);
                return TelegramWebAppValidationResult.Failure("Data expired");
            }

            // Создаём data-check-string
            // Все параметры кроме hash, отсортированные по алфавиту, в формате key=value через \n
            var dataCheckString = string.Join("\n",
                data.AllKeys
                    .Where(k => k != "hash" && k != null)
                    .OrderBy(k => k)
                    .Select(k => $"{k}={data[k]}"));

            // Вычисляем HMAC-SHA256
            // secret_key = HMAC_SHA256(bot_token, "WebAppData")
            var secretKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes("WebAppData"),
                Encoding.UTF8.GetBytes(BotToken));

            // hash = HMAC_SHA256(secret_key, data_check_string)
            var computedHashBytes = HMACSHA256.HashData(
                secretKey,
                Encoding.UTF8.GetBytes(dataCheckString));

            var computedHash = Convert.ToHexString(computedHashBytes).ToLowerInvariant();

            // Сравниваем хэши
            if (!string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid hash. Expected: {Expected}, Got: {Got}", computedHash, hash);
                return TelegramWebAppValidationResult.Failure("Invalid signature");
            }

            // Парсим данные пользователя
            var userJson = data["user"];
            if (string.IsNullOrEmpty(userJson))
            {
                _logger.LogWarning("User data not found in initData");
                return TelegramWebAppValidationResult.Failure("User data not found");
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            var user = JsonSerializer.Deserialize<TelegramWebAppUserData>(userJson, jsonOptions);
            if (user == null || user.Id == 0)
            {
                _logger.LogWarning("Failed to parse user data from initData");
                return TelegramWebAppValidationResult.Failure("Failed to parse user data");
            }

            _logger.LogInformation("Successfully validated initData for Telegram user {TelegramId}", user.Id);
            return TelegramWebAppValidationResult.Success(user, authDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating initData");
            return TelegramWebAppValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }
}
