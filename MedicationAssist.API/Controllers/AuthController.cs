using System.Security.Claims;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicationAssist.API.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebLoginTokenService _webLoginTokenService;
    private readonly ITelegramLoginService _telegramLoginService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IWebLoginTokenService webLoginTokenService,
        ITelegramLoginService telegramLoginService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _webLoginTokenService = webLoginTokenService;
        _telegramLoginService = telegramLoginService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="dto">Данные для регистрации</param>
    /// <returns>JWT токен и информация о пользователе</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        _logger.LogInformation("Попытка регистрации пользователя с email {Email}", dto.Email);

        var result = await _authService.RegisterAsync(dto);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ошибка регистрации: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Пользователь успешно зарегистрирован: {Email}", dto.Email);
        return CreatedAtAction(nameof(Register), result.Data);
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    /// <param name="dto">Данные для входа</param>
    /// <returns>JWT токен и информация о пользователе</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Попытка входа пользователя с email {Email}", dto.Email);

        var result = await _authService.LoginAsync(dto);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ошибка входа для email {Email}: {Error}", dto.Email, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Пользователь успешно вошел: {Email}", dto.Email);
        return Ok(result.Data);
    }

    /// <summary>
    /// Обновление access токена с помощью refresh токена
    /// </summary>
    /// <param name="dto">Refresh токен</param>
    /// <returns>Новые access и refresh токены</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        _logger.LogInformation("Попытка обновления токена");

        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ошибка обновления токена: {Error}", result.Error);
            return Unauthorized(new { error = result.Error });
        }

        _logger.LogInformation("Токен успешно обновлен");
        return Ok(result.Data);
    }

    /// <summary>
    /// Отзыв refresh токена (выход с текущего устройства)
    /// </summary>
    /// <param name="dto">Refresh токен для отзыва</param>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
    {
        _logger.LogInformation("Попытка отзыва refresh токена");

        var result = await _authService.RevokeTokenAsync(dto.RefreshToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ошибка отзыва токена: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Refresh токен успешно отозван");
        return Ok(new { message = "Токен успешно отозван" });
    }

    /// <summary>
    /// Отзыв всех refresh токенов пользователя (выход со всех устройств)
    /// </summary>
    [HttpPost("revoke-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllTokens()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Не удалось определить пользователя" });
        }

        _logger.LogInformation("Попытка отзыва всех токенов для пользователя {UserId}", userId);

        var result = await _authService.RevokeAllTokensAsync(userId);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ошибка отзыва токенов: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Все токены отозваны для пользователя {UserId}", userId);
        return Ok(new { message = "Все токены успешно отозваны" });
    }

    /// <summary>
    /// Автоматический вход на веб-сайт через токен из Telegram бота
    /// </summary>
    /// <param name="dto">Токен веб-логина</param>
    /// <returns>JWT токен и информация о пользователе</returns>
    [HttpPost("telegram-web-login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TelegramWebLogin([FromBody] TelegramWebLoginDto dto)
    {
        _logger.LogInformation("Попытка веб-логина через Telegram токен");

        // Валидируем и потребляем токен
        var userId = await _webLoginTokenService.ValidateAndConsumeTokenAsync(dto.Token);

        if (userId == null)
        {
            _logger.LogWarning("Недействительный или истекший веб-логин токен");
            return Unauthorized(new { error = "Недействительный или истекший токен" });
        }

        // Получаем пользователя
        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
        {
            _logger.LogError("Пользователь {UserId} не найден после валидации токена", userId);
            return Unauthorized(new { error = "Пользователь не найден" });
        }

        // Генерируем JWT токены
        var tokens = _jwtTokenService.GenerateTokens(user);

        // Сохраняем refresh токен
        await _refreshTokenService.CreateTokenAsync(
            user.Id,
            tokens.RefreshToken,
            tokens.RefreshTokenExpires);

        var response = new AuthResponseDto
        {
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            TokenExpires = tokens.AccessTokenExpires,
            User = new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.TelegramUserId,
                user.TelegramUsername,
                user.TimeZoneId,
                user.CreatedAt,
                user.UpdatedAt)
        };

        _logger.LogInformation("Успешный веб-логин через Telegram для пользователя {UserId}", userId);
        return Ok(response);
    }

    /// <summary>
    /// Инициировать вход через Telegram бот (генерирует токен и deep link)
    /// </summary>
    /// <returns>Токен, deep link и URL для polling</returns>
    [HttpPost("telegram-login-init")]
    [ProducesResponseType(typeof(TelegramLoginInitResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateTelegramLogin()
    {
        _logger.LogInformation("Инициализация входа через Telegram");

        // Генерируем анонимный токен для авторизации
        var token = await _telegramLoginService.GenerateAnonymousTokenAsync();

        // Получаем имя бота из конфигурации
        var botUsername = _configuration["TelegramBot:BotUsername"];
        if (string.IsNullOrEmpty(botUsername))
        {
            _logger.LogError("TelegramBot:BotUsername не настроен в конфигурации");
            return BadRequest(new { error = "Telegram бот не настроен" });
        }

        // Формируем deep link для перехода в бот
        var deepLink = $"https://t.me/{botUsername}?start=weblogin_{token}";

        var response = new TelegramLoginInitResponseDto
        {
            Token = token,
            DeepLink = deepLink,
            ExpiresInMinutes = 5,
            PollUrl = $"/api/auth/telegram-login-poll/{token}"
        };

        _logger.LogInformation("Сгенерирован токен для входа через Telegram: {Token}", token);
        return Ok(response);
    }

    /// <summary>
    /// Polling endpoint для проверки статуса авторизации через Telegram
    /// </summary>
    /// <param name="token">Токен авторизации</param>
    /// <returns>Статус авторизации и токены при успехе</returns>
    [HttpGet("telegram-login-poll/{token}")]
    [ProducesResponseType(typeof(TelegramLoginPollResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PollTelegramLogin(string token)
    {
        _logger.LogDebug("Polling статуса авторизации для токена: {Token}", token);

        // Проверяем статус авторизации
        var authStatus = await _telegramLoginService.CheckAuthorizationStatusAsync(token);

        if (authStatus == null)
        {
            // Токен не найден или истек
            _logger.LogDebug("Токен не найден или истек: {Token}", token);
            return Ok(new TelegramLoginPollResponseDto { Status = "expired" });
        }

        if (!authStatus.IsAuthorized)
        {
            // Авторизация еще не завершена
            _logger.LogDebug("Ожидание авторизации для токена: {Token}", token);
            return Ok(new TelegramLoginPollResponseDto { Status = "pending" });
        }

        // Пользователь авторизован - генерируем JWT токены
        _logger.LogInformation("Пользователь {UserId} авторизован через Telegram", authStatus.UserId);

        var user = await _userRepository.GetByIdAsync(authStatus.UserId!.Value);
        if (user == null)
        {
            _logger.LogError("Пользователь {UserId} не найден после авторизации", authStatus.UserId);
            return Ok(new TelegramLoginPollResponseDto { Status = "expired" });
        }

        // Генерируем JWT токены
        var tokens = _jwtTokenService.GenerateTokens(user);

        // Сохраняем refresh токен
        await _refreshTokenService.CreateTokenAsync(
            user.Id,
            tokens.RefreshToken,
            tokens.RefreshTokenExpires);

        var response = new TelegramLoginPollResponseDto
        {
            Status = "authorized",
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            TokenExpires = tokens.AccessTokenExpires,
            User = new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.TelegramUserId,
                user.TelegramUsername,
                user.TimeZoneId,
                user.CreatedAt,
                user.UpdatedAt)
        };

        _logger.LogInformation("Успешный вход через Telegram для пользователя {UserId}", user.Id);
        return Ok(response);
    }
}

