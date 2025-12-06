using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
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
}

