using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicationAssist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение всех пользователей");
        var result = await _userService.GetAllAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение пользователя {UserId}", id);
        var result = await _userService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Получить пользователя по email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение пользователя по email {Email}", email);
        var result = await _userService.GetByEmailAsync(email, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание пользователя с email {Email}", dto.Email);
        var result = await _userService.CreateAsync(dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Обновить пользователя
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на обновление пользователя {UserId}", id);
        var result = await _userService.UpdateAsync(id, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на удаление пользователя {UserId}", id);
        var result = await _userService.DeleteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Получить пользователя по Telegram ID
    /// </summary>
    [HttpGet("by-telegram/{telegramUserId:long}")]
    public async Task<ActionResult<UserDto>> GetByTelegramId(long telegramUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение пользователя по Telegram ID {TelegramUserId}", telegramUserId);
        var result = await _userService.GetByTelegramIdAsync(telegramUserId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Привязать Telegram аккаунт к пользователю
    /// </summary>
    [HttpPut("{userId:guid}/telegram-link")]
    public async Task<ActionResult<UserDto>> LinkTelegram(Guid userId, [FromBody] LinkTelegramDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на привязку Telegram {TelegramUserId} к пользователю {UserId}", dto.TelegramUserId, userId);

        // Проверка: только владелец аккаунта или админ может привязывать Telegram
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var currentUserRoleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

        if (currentUserIdClaim == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var currentUserId = Guid.Parse(currentUserIdClaim.Value);
        var currentUserRole = currentUserRoleClaim?.Value;

        if (currentUserId != userId && currentUserRole != "Admin")
        {
            _logger.LogWarning("User {CurrentUserId} tried to link Telegram for user {UserId}", currentUserId, userId);
            return Forbid();
        }

        var result = await _userService.LinkTelegramAsync(userId, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Отвязать Telegram аккаунт от пользователя
    /// </summary>
    [HttpDelete("{userId:guid}/telegram-link")]
    public async Task<ActionResult<UserDto>> UnlinkTelegram(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на отвязку Telegram от пользователя {UserId}", userId);

        // Проверка: только владелец аккаунта или админ может отвязывать Telegram
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var currentUserRoleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

        if (currentUserIdClaim == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var currentUserId = Guid.Parse(currentUserIdClaim.Value);
        var currentUserRole = currentUserRoleClaim?.Value;

        if (currentUserId != userId && currentUserRole != "Admin")
        {
            _logger.LogWarning("User {CurrentUserId} tried to unlink Telegram for user {UserId}", currentUserId, userId);
            return Forbid();
        }

        var result = await _userService.UnlinkTelegramAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

