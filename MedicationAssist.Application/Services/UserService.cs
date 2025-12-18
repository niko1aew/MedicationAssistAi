using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILinkTokenService _linkTokenService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILinkTokenService linkTokenService, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _linkTokenService = linkTokenService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User с ID {UserId} not found", id);
                return Result<UserDto>.Failure("User not found");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting user {UserId}", id);
            return Result<UserDto>.Failure($"Error while getting user: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User с email {Email} not found", email);
                return Result<UserDto>.Failure("User not found");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting user по email {Email}", email);
            return Result<UserDto>.Failure($"Error while getting user: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var userDtos = users.Select(MapToDto);

            _logger.LogInformation("Retrieved {Count} пользователей", userDtos.Count());
            return Result<IEnumerable<UserDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while получении user list");
            return Result<IEnumerable<UserDto>>.Failure($"Error while получении user list: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, не существует ли пользователь с таким email
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempt создать пользователя с существующим email {Email}", dto.Email);
                return Result<UserDto>.Failure("User with this email already exists");
            }

            // УСТАРЕВШИЙ МЕТОД: Используйте AuthService.RegisterAsync вместо этого
            // Временно создаем с пустым хэшем пароля для обратной совместимости
            var user = new User(dto.Name, dto.Email, "DEPRECATED_USE_AUTH_API", Domain.Entities.UserRole.User);
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created новый пользователь {UserId} с email {Email}", user.Id, user.Email);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error при creating user");
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating user");
            return Result<UserDto>.Failure($"Error while creating user: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempt обновить несуществующего пользователя {UserId}", id);
                return Result<UserDto>.Failure("User not found");
            }

            // Проверяем, не занят ли email другим пользователем
            if (user.Email != dto.Email)
            {
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
                if (existingUser != null && existingUser.Id != id)
                {
                    _logger.LogWarning("Attempt change email to an already existing one {Email}", dto.Email);
                    return Result<UserDto>.Failure("User with this email already exists");
                }
            }

            user.SetName(dto.Name);
            user.SetEmail(dto.Email);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated пользователь {UserId}", id);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error при updating user {UserId}", id);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating user {UserId}", id);
            return Result<UserDto>.Failure($"Error while updating user: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> GetByTelegramIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User with Telegram ID {TelegramUserId} not found", telegramUserId);
                return Result<UserDto>.Failure("User not found");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting user by Telegram ID {TelegramUserId}", telegramUserId);
            return Result<UserDto>.Failure($"Error while getting user: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> LinkTelegramAsync(Guid userId, LinkTelegramDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, не привязан ли уже этот Telegram ID к другому пользователю
            var existingUser = await _unitOfWork.Users.GetByTelegramIdAsync(dto.TelegramUserId, cancellationToken);
            if (existingUser != null && existingUser.Id != userId)
            {
                _logger.LogWarning("Telegram ID {TelegramUserId} is already linked to another account", dto.TelegramUserId);
                return Result<UserDto>.Failure("This Telegram account is already linked to another user");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Result<UserDto>.Failure("User not found");
            }

            user.SetTelegramAccount(dto.TelegramUserId, dto.TelegramUsername);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Telegram account {TelegramUserId} linked to user {UserId}", dto.TelegramUserId, userId);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while linking Telegram to user {UserId}", userId);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while linking Telegram to user {UserId}", userId);
            return Result<UserDto>.Failure($"Error while linking Telegram: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> UnlinkTelegramAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Result<UserDto>.Failure("User not found");
            }

            user.RemoveTelegramAccount();

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Telegram account unlinked from user {UserId}", userId);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while unlinking Telegram from user {UserId}", userId);
            return Result<UserDto>.Failure($"Error while unlinking Telegram: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> SetTimeZoneAsync(Guid userId, string timeZoneId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Result<UserDto>.Failure("User not found");
            }

            user.SetTimeZone(timeZoneId);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Timezone updated for user {UserId} to {TimeZoneId}", userId, timeZoneId);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while setting timezone for user {UserId}", userId);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while setting timezone for user {UserId}", userId);
            return Result<UserDto>.Failure($"Error while setting timezone: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Users.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Attempt удалить несуществующего пользователя {UserId}", id);
                return Result.Failure("User not found");
            }

            await _unitOfWork.Users.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted пользователь {UserId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting user {UserId}", id);
            return Result.Failure($"Error while deleting user: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateLinkTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Result<string>.Failure("User not found");
            }

            // Делегируем генерацию токена в Infrastructure сервис
            var token = await _linkTokenService.GenerateTokenAsync(userId, cancellationToken);

            _logger.LogInformation("Generated link token for user {UserId}", userId);
            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while generating link token for user {UserId}", userId);
            return Result<string>.Failure($"Error while generating link token: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> LinkTelegramByTokenAsync(string token, LinkTelegramDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Валидируем токен через Infrastructure сервис
            var userId = await _linkTokenService.ValidateAndConsumeTokenAsync(token, cancellationToken);
            if (userId == null)
            {
                _logger.LogWarning("Link token {Token} is invalid or expired", token);
                return Result<UserDto>.Failure("Invalid or expired link token");
            }

            // Проверяем, не привязан ли уже этот Telegram ID к другому пользователю
            var existingUser = await _unitOfWork.Users.GetByTelegramIdAsync(dto.TelegramUserId, cancellationToken);
            if (existingUser != null && existingUser.Id != userId)
            {
                _logger.LogWarning("Telegram ID {TelegramUserId} is already linked to another account", dto.TelegramUserId);
                return Result<UserDto>.Failure("This Telegram account is already linked to another user");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for valid token", userId);
                return Result<UserDto>.Failure("User not found");
            }

            user.SetTelegramAccount(dto.TelegramUserId, dto.TelegramUsername);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Telegram account {TelegramUserId} linked to user {UserId} via token", dto.TelegramUserId, user.Id);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while linking Telegram via token");
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while linking Telegram via token");
            return Result<UserDto>.Failure($"Error while linking Telegram: {ex.Message}");
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.TelegramUserId,
            user.TelegramUsername,
            user.TimeZoneId,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}

