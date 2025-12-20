using System.Security.Cryptography;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.Infrastructure.Data;
using MedicationAssist.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace MedicationAssist.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с токенами веб-логина через Telegram
/// </summary>
public class TelegramLoginTokenRepository : ITelegramLoginTokenRepository
{
    private readonly ApplicationDbContext _context;
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromMinutes(5);

    public TelegramLoginTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateTokenAsync(CancellationToken ct = default)
    {
        var tokenValue = GenerateSecureToken(32);

        var token = new TelegramLoginToken
        {
            Token = tokenValue,
            IsAuthorized = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
        };

        _context.TelegramLoginTokens.Add(token);
        await _context.SaveChangesAsync(ct);

        return tokenValue;
    }

    public async Task SetAuthorizedAsync(string token, Guid userId, CancellationToken ct = default)
    {
        var existingToken = await _context.TelegramLoginTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (existingToken == null)
        {
            return;
        }

        existingToken.IsAuthorized = true;
        existingToken.UserId = userId;
        existingToken.AuthorizedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    public async Task<TelegramLoginTokenInfo?> GetTokenInfoAsync(string token, CancellationToken ct = default)
    {
        var existingToken = await _context.TelegramLoginTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (existingToken == null || existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return new TelegramLoginTokenInfo
        {
            IsAuthorized = existingToken.IsAuthorized,
            UserId = existingToken.UserId,
            CreatedAt = existingToken.CreatedAt,
            AuthorizedAt = existingToken.AuthorizedAt,
            ExpiresAt = existingToken.ExpiresAt
        };
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var expiredTokens = await _context.TelegramLoginTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        _context.TelegramLoginTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(ct);
    }

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
