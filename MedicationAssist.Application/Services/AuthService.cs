using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Сервис аутентификации
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        _logger.LogInformation("Попытка регистрации пользователя с email {Email}", dto.Email);

        // Проверка существования пользователя
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Попытка регистрации с существующим email {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Пользователь с таким email уже существует");
        }

        // Валидация пароля
        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<AuthResponseDto>.Failure("Пароль не может быть пустым");

        if (dto.Password.Length < 6)
            return Result<AuthResponseDto>.Failure("Пароль должен содержать минимум 6 символов");

        if (dto.Password.Length > 100)
            return Result<AuthResponseDto>.Failure("Пароль не может превышать 100 символов");

        try
        {
            // Хэширование пароля
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            // Создание пользователя
            var user = new User(dto.Name, dto.Email, passwordHash, UserRole.User);

            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Генерация токена
            var token = _jwtTokenService.GenerateToken(user);

            _logger.LogInformation("Пользователь {UserId} с email {Email} успешно зарегистрирован", user.Id, user.Email);

            var response = new AuthResponseDto
            {
                Token = token,
                User = new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt, user.UpdatedAt)
            };

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации пользователя");
            return Result<AuthResponseDto>.Failure($"Ошибка регистрации: {ex.Message}");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        _logger.LogInformation("Попытка входа пользователя с email {Email}", dto.Email);

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Попытка входа с несуществующим email {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Неверный email или пароль");
        }

        // Проверка пароля
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Неверный пароль для пользователя {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Неверный email или пароль");
        }

        // Генерация токена
        var token = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation("Пользователь {UserId} успешно вошел в систему", user.Id);

        var response = new AuthResponseDto
        {
            Token = token,
            User = new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt, user.UpdatedAt)
        };

        return Result<AuthResponseDto>.Success(response);
    }
}

