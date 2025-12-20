using System.Security.Cryptography;
using MedicationAssist.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация сервиса управления состоянием авторизации через Telegram
/// </summary>
public class TelegramLoginService : ITelegramLoginService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TelegramLoginService> _logger;
    private const string CacheKeyPrefix = "tg_login_";
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AuthorizedTokenExpiration = TimeSpan.FromMinutes(1);

    public TelegramLoginService(
        IMemoryCache cache,
        ILogger<TelegramLoginService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> GenerateAnonymousTokenAsync()
    {
        var token = GenerateSecureToken(32);
        var cacheKey = GetCacheKey(token);

        var status = new TelegramLoginStatus
        {
            IsAuthorized = false,
            CreatedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, status, TokenExpiration);

        _logger.LogInformation("Generated anonymous Telegram login token with expiration {Expiration}", TokenExpiration);

        return Task.FromResult(token);
    }

    /// <inheritdoc/>
    public Task SetAuthorizedAsync(string token, Guid userId)
    {
        var cacheKey = GetCacheKey(token);

        var status = new TelegramLoginStatus
        {
            IsAuthorized = true,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            AuthorizedAt = DateTime.UtcNow
        };

        // Update with shorter expiration after authorization
        _cache.Set(cacheKey, status, AuthorizedTokenExpiration);

        _logger.LogInformation("Telegram login token authorized for user {UserId}", userId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<TelegramLoginStatus?> CheckAuthorizationStatusAsync(string token)
    {
        var cacheKey = GetCacheKey(token);

        if (_cache.TryGetValue<TelegramLoginStatus>(cacheKey, out var status))
        {
            return Task.FromResult<TelegramLoginStatus?>(status);
        }

        return Task.FromResult<TelegramLoginStatus?>(null);
    }

    private static string GetCacheKey(string token) => $"{CacheKeyPrefix}{token}";

    private static string GenerateSecureToken(int length)
    {
        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
