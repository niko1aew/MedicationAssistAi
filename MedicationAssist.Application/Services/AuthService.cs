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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        _logger.LogInformation("Attempting to register user with email {Email}", dto.Email);

        // Проверка существования пользователя
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Attempt to register with existing email {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("User with this email already exists");
        }

        // Валидация пароля
        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<AuthResponseDto>.Failure("Password cannot be empty");

        if (dto.Password.Length < 6)
            return Result<AuthResponseDto>.Failure("Password must contain at least 6 characters");

        if (dto.Password.Length > 100)
            return Result<AuthResponseDto>.Failure("Password cannot exceed 100 characters");

        try
        {
            // Хэширование пароля
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            // Создание пользователя
            var user = new User(dto.Name, dto.Email, passwordHash, UserRole.User);

            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Генерация токенов
            var tokens = _jwtTokenService.GenerateTokens(user);

            // Сохранение refresh токена
            await _refreshTokenService.CreateTokenAsync(
                user.Id,
                tokens.RefreshToken,
                tokens.RefreshTokenExpires);

            _logger.LogInformation("User {UserId} with email {Email} successfully registered", user.Id, user.Email);

            var response = new AuthResponseDto
            {
                Token = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                TokenExpires = tokens.AccessTokenExpires,
                User = new UserDto(user.Id, user.Name, user.Email, user.Role, user.TelegramUserId, user.TelegramUsername, user.CreatedAt, user.UpdatedAt)
            };

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering user");
            return Result<AuthResponseDto>.Failure($"Registration error: {ex.Message}");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        _logger.LogInformation("Attempting to login user with email {Email}", dto.Email);

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Attempt to login with non-existent email {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Invalid email or password");
        }

        // Проверка пароля
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Invalid email or password");
        }

        // Генерация токенов
        var tokens = _jwtTokenService.GenerateTokens(user);

        // Сохранение refresh токена
        await _refreshTokenService.CreateTokenAsync(
            user.Id,
            tokens.RefreshToken,
            tokens.RefreshTokenExpires);

        _logger.LogInformation("User {UserId} successfully logged in", user.Id);

        var response = new AuthResponseDto
        {
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            TokenExpires = tokens.AccessTokenExpires,
            User = new UserDto(user.Id, user.Name, user.Email, user.Role, user.TelegramUserId, user.TelegramUsername, user.CreatedAt, user.UpdatedAt)
        };

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to refresh token");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthResponseDto>.Failure("Refresh token is not specified");
        }

        // Получаем существующий токен
        var existingToken = await _refreshTokenService.GetByTokenAsync(refreshToken);
        if (existingToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            return Result<AuthResponseDto>.Failure("Invalid refresh token");
        }

        if (!existingToken.IsActive)
        {
            _logger.LogWarning("Attempt to use inactive refresh token for user {UserId}", existingToken.UserId);
            return Result<AuthResponseDto>.Failure("Refresh token has expired or been revoked");
        }

        // Получаем пользователя
        var user = await _userRepository.GetByIdAsync(existingToken.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found while refreshing token", existingToken.UserId);
            return Result<AuthResponseDto>.Failure("User not found");
        }

        // Генерируем новые токены
        var tokens = _jwtTokenService.GenerateTokens(user);

        // Ротация: отзываем старый токен и создаём новый
        await _refreshTokenService.RotateTokenAsync(
            refreshToken,
            user.Id,
            tokens.RefreshToken,
            tokens.RefreshTokenExpires);

        _logger.LogInformation("Tokens successfully refreshed for user {UserId}", user.Id);

        var response = new AuthResponseDto
        {
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            TokenExpires = tokens.AccessTokenExpires,
            User = new UserDto(user.Id, user.Name, user.Email, user.Role, user.TelegramUserId, user.TelegramUsername, user.CreatedAt, user.UpdatedAt)
        };

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to revoke refresh token");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure("Refresh token is not specified");
        }

        var existingToken = await _refreshTokenService.GetByTokenAsync(refreshToken);
        if (existingToken == null)
        {
            return Result.Failure("Refresh token not found");
        }

        if (!existingToken.IsActive)
        {
            return Result.Failure("Token has already been revoked or expired");
        }

        await _refreshTokenService.RevokeTokenAsync(refreshToken);

        _logger.LogInformation("Refresh token successfully revoked for user {UserId}", existingToken.UserId);

        return Result.Success();
    }

    public async Task<Result> RevokeAllTokensAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to revoke all tokens for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        await _refreshTokenService.RevokeAllUserTokensAsync(userId);

        _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);

        return Result.Success();
    }
}

