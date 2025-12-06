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
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден", id);
                return Result<UserDto>.Failure("Пользователь не найден");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
            return Result<UserDto>.Failure($"Ошибка при получении пользователя: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с email {Email} не найден", email);
                return Result<UserDto>.Failure("Пользователь не найден");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователя по email {Email}", email);
            return Result<UserDto>.Failure($"Ошибка при получении пользователя: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var userDtos = users.Select(MapToDto);
            
            _logger.LogInformation("Получено {Count} пользователей", userDtos.Count());
            return Result<IEnumerable<UserDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка пользователей");
            return Result<IEnumerable<UserDto>>.Failure($"Ошибка при получении списка пользователей: {ex.Message}");
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
                _logger.LogWarning("Попытка создать пользователя с существующим email {Email}", dto.Email);
                return Result<UserDto>.Failure("Пользователь с таким email уже существует");
            }

            // УСТАРЕВШИЙ МЕТОД: Используйте AuthService.RegisterAsync вместо этого
            // Временно создаем с пустым хэшем пароля для обратной совместимости
            var user = new User(dto.Name, dto.Email, "DEPRECATED_USE_AUTH_API", Domain.Entities.UserRole.User);
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Создан новый пользователь {UserId} с email {Email}", user.Id, user.Email);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при создании пользователя");
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании пользователя");
            return Result<UserDto>.Failure($"Ошибка при создании пользователя: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Попытка обновить несуществующего пользователя {UserId}", id);
                return Result<UserDto>.Failure("Пользователь не найден");
            }

            // Проверяем, не занят ли email другим пользователем
            if (user.Email != dto.Email)
            {
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
                if (existingUser != null && existingUser.Id != id)
                {
                    _logger.LogWarning("Попытка изменить email на уже существующий {Email}", dto.Email);
                    return Result<UserDto>.Failure("Пользователь с таким email уже существует");
                }
            }

            user.SetName(dto.Name);
            user.SetEmail(dto.Email);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Обновлен пользователь {UserId}", id);
            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при обновлении пользователя {UserId}", id);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
            return Result<UserDto>.Failure($"Ошибка при обновлении пользователя: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Users.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Попытка удалить несуществующего пользователя {UserId}", id);
                return Result.Failure("Пользователь не найден");
            }

            await _unitOfWork.Users.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Удален пользователь {UserId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
            return Result.Failure($"Ошибка при удалении пользователя: {ex.Message}");
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}

