using System.Security.Claims;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
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
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
}

