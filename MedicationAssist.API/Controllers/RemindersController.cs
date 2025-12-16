using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicationAssist.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/reminders")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(IReminderService reminderService, ILogger<RemindersController> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all reminders for a user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReminderDto>>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get reminders for user {UserId}", userId);

        var result = await _reminderService.GetByUserIdAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get reminder by id
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReminderDto>> GetById(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get reminder {ReminderId} for user {UserId}", id, userId);

        var result = await _reminderService.GetByUserIdAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        var reminder = result.Data!.FirstOrDefault(r => r.Id == id);
        if (reminder == null)
        {
            return NotFound(new { error = "Reminder not found" });
        }

        return Ok(reminder);
    }

    /// <summary>
    /// Create a reminder for a user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Create(Guid userId, [FromBody] CreateReminderDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create reminder for user {UserId}", userId);

        var result = await _reminderService.CreateAsync(userId, dto, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { userId, id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Delete a reminder
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete reminder {ReminderId} for user {UserId}", id, userId);

        // Ensure reminder belongs to user
        var existing = await _reminderService.GetByUserIdAsync(userId, cancellationToken);
        if (!existing.IsSuccess)
        {
            return NotFound(new { error = existing.Error });
        }

        if (existing.Data!.All(r => r.Id != id))
        {
            return NotFound(new { error = "Reminder not found" });
        }

        var result = await _reminderService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

