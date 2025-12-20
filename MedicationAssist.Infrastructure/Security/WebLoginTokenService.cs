using MedicationAssist.Application.Services;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация сервиса для работы с токенами веб-логина (Infrastructure Layer)
/// </summary>
public class WebLoginTokenService : IWebLoginTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebLoginTokenService> _logger;
    private const int TokenExpirationMinutes = 5; // Короткоживущий токен для немедленного использования
    private const int CleanupDaysThreshold = 7;

    public WebLoginTokenService(ApplicationDbContext context, ILogger<WebLoginTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Генерируем новый токен каждый раз (не переиспользуем как в LinkToken)
            // Это безопаснее для веб-логина, так как токен короткоживущий
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes);

            var webLoginToken = new WebLoginToken(token, userId, expiresAt);

            await _context.WebLoginTokens.AddAsync(webLoginToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated new web login token for user {UserId}, expires at {ExpiresAt}",
                userId, expiresAt);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating web login token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Guid?> ValidateAndConsumeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var webLoginToken = await _context.WebLoginTokens
                .FirstOrDefaultAsync(wlt => wlt.Token == token, cancellationToken);

            if (webLoginToken == null)
            {
                _logger.LogWarning("Web login token {Token} not found", token);
                return null;
            }

            if (!webLoginToken.IsValid())
            {
                _logger.LogWarning("Web login token {Token} is invalid or expired", token);
                return null;
            }

            // Помечаем токен как использованный
            webLoginToken.MarkAsUsed();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Web login token {Token} consumed for user {UserId}", token, webLoginToken.UserId);

            return webLoginToken.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating web login token {Token}", token);
            throw;
        }
    }

    public async Task<string?> GetActiveTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var webLoginToken = await GetActiveTokenInternalAsync(userId, cancellationToken);
        return webLoginToken?.Token;
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var threshold = DateTime.UtcNow.AddDays(-CleanupDaysThreshold);

            var expiredTokens = await _context.WebLoginTokens
                .Where(wlt => (wlt.ExpiresAt < DateTime.UtcNow || wlt.IsUsed) && wlt.CreatedAt < threshold)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                _context.WebLoginTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired web login tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired web login tokens");
            throw;
        }
    }

    private async Task<WebLoginToken?> GetActiveTokenInternalAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.WebLoginTokens
            .Where(wlt => wlt.UserId == userId && !wlt.IsUsed && wlt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(wlt => wlt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        // Генерируем криптографически стойкий токен фиксированной длины
        var bytes = new byte[24]; // 24 байта дадут ровно 32 символа в Base64 URL-safe
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // Используем Base64 URL-safe encoding (заменяем проблемные символы)
        var token = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", ""); // Убираем padding

        return token;
    }
}
