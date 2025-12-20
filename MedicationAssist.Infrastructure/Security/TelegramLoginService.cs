using System.Security.Cryptography;
using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация сервиса управления состоянием авторизации через Telegram
/// </summary>
public class TelegramLoginService : ITelegramLoginService
{
    private readonly ITelegramLoginTokenRepository _tokenRepository;
    private readonly ILogger<TelegramLoginService> _logger;

    public TelegramLoginService(
        ITelegramLoginTokenRepository tokenRepository,
        ILogger<TelegramLoginService> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAnonymousTokenAsync()
    {
        var token = await _tokenRepository.CreateTokenAsync();
        _logger.LogInformation("Generated anonymous Telegram login token");
        return token;
    }

    /// <inheritdoc/>
    public async Task SetAuthorizedAsync(string token, Guid userId)
    {
        await _tokenRepository.SetAuthorizedAsync(token, userId);
        _logger.LogInformation("Telegram login token authorized for user {UserId}", userId);
    }

    /// <inheritdoc/>
    public async Task<TelegramLoginStatus?> CheckAuthorizationStatusAsync(string token)
    {
        var tokenInfo = await _tokenRepository.GetTokenInfoAsync(token);

        if (tokenInfo == null)
        {
            return null;
        }

        return new TelegramLoginStatus
        {
            IsAuthorized = tokenInfo.IsAuthorized,
            UserId = tokenInfo.UserId,
            CreatedAt = tokenInfo.CreatedAt,
            AuthorizedAt = tokenInfo.AuthorizedAt
        };
    }
}
