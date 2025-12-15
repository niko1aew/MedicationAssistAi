using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Repositories;

namespace MedicationAssist.Infrastructure.Security;

/// <summary>
/// Реализация сервиса для работы с refresh токенами
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenService(IRefreshTokenRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshTokenInfo> CreateTokenAsync(Guid userId, string tokenValue, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = tokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        await _repository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToInfo(refreshToken);
    }

    public async Task<RefreshTokenInfo?> GetByTokenAsync(string token)
    {
        var refreshToken = await _repository.GetByTokenAsync(token);
        return refreshToken == null ? null : MapToInfo(refreshToken);
    }

    public async Task<RefreshTokenInfo> RotateTokenAsync(string oldToken, Guid userId, string newTokenValue, DateTime expiresAt)
    {
        var existingToken = await _repository.GetByTokenAsync(oldToken);
        
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = newTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        // Отзываем старый токен и связываем с новым
        if (existingToken != null && existingToken.IsActive)
        {
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByTokenId = newRefreshToken.Id;
            await _repository.UpdateAsync(existingToken);
        }

        await _repository.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToInfo(newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token)
    {
        var refreshToken = await _repository.GetByTokenAsync(token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        await _repository.RevokeAllForUserAsync(userId);
        await _unitOfWork.SaveChangesAsync();
    }

    private static RefreshTokenInfo MapToInfo(RefreshToken token)
    {
        return new RefreshTokenInfo
        {
            Id = token.Id,
            UserId = token.UserId,
            Token = token.Token,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
            IsActive = token.IsActive
        };
    }
}

