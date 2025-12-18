using MedicationAssist.Application.Services;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация сервиса для работы с токенами привязки Telegram (Infrastructure Layer)
/// </summary>
public class LinkTokenService : ILinkTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LinkTokenService> _logger;
    private const int TokenExpirationMinutes = 15;
    private const int CleanupDaysThreshold = 7;

    public LinkTokenService(ApplicationDbContext context, ILogger<LinkTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем наличие активного токена
            var existingToken = await GetActiveTokenInternalAsync(userId, cancellationToken);
            if (existingToken != null)
            {
                _logger.LogInformation("Returning existing active token for user {UserId}", userId);
                return existingToken.Token;
            }

            // Генерируем новый токен
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes);

            var linkToken = new LinkToken(token, userId, expiresAt);

            await _context.LinkTokens.AddAsync(linkToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated new link token for user {UserId}, expires at {ExpiresAt}",
                userId, expiresAt);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating link token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Guid?> ValidateAndConsumeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var linkToken = await _context.LinkTokens
                .FirstOrDefaultAsync(lt => lt.Token == token, cancellationToken);

            if (linkToken == null)
            {
                _logger.LogWarning("Link token {Token} not found", token);
                return null;
            }

            if (!linkToken.IsValid())
            {
                _logger.LogWarning("Link token {Token} is invalid or expired", token);
                return null;
            }

            // Помечаем токен как использованный
            linkToken.MarkAsUsed();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Link token {Token} consumed for user {UserId}", token, linkToken.UserId);

            return linkToken.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating link token {Token}", token);
            throw;
        }
    }

    public async Task<string?> GetActiveTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var linkToken = await GetActiveTokenInternalAsync(userId, cancellationToken);
        return linkToken?.Token;
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var threshold = DateTime.UtcNow.AddDays(-CleanupDaysThreshold);

            var expiredTokens = await _context.LinkTokens
                .Where(lt => (lt.ExpiresAt < DateTime.UtcNow || lt.IsUsed) && lt.CreatedAt < threshold)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                _context.LinkTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired link tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired link tokens");
            throw;
        }
    }

    private async Task<LinkToken?> GetActiveTokenInternalAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.LinkTokens
            .Where(lt => lt.UserId == userId && !lt.IsUsed && lt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(lt => lt.CreatedAt)
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
