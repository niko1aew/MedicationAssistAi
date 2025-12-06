using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicationAssist.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/intakes")]
[Authorize]
public class MedicationIntakesController : ControllerBase
{
    private readonly IMedicationIntakeService _intakeService;
    private readonly ILogger<MedicationIntakesController> _logger;

    public MedicationIntakesController(IMedicationIntakeService intakeService, ILogger<MedicationIntakesController> logger)
    {
        _intakeService = intakeService;
        _logger = logger;
    }

    /// <summary>
    /// Получить все приемы лекарств пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicationIntakeDto>>> GetByUserId(
        Guid userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? medicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение приемов лекарств пользователя {UserId}", userId);
        
        var filter = new MedicationIntakeFilterDto(fromDate, toDate, medicationId);
        var result = await _intakeService.GetByUserIdAsync(userId, filter, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Получить запись о приеме по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MedicationIntakeDto>> GetById(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение записи о приеме {IntakeId} пользователя {UserId}", id, userId);
        var result = await _intakeService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        // Проверяем, что запись принадлежит пользователю
        if (result.Data!.UserId != userId)
        {
            return Forbid();
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Создать новую запись о приеме лекарства
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MedicationIntakeDto>> Create(
        Guid userId, 
        [FromBody] CreateMedicationIntakeDto dto, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание записи о приеме лекарства для пользователя {UserId}", userId);
        var result = await _intakeService.CreateAsync(userId, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { userId, id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Обновить запись о приеме
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MedicationIntakeDto>> Update(
        Guid userId, 
        Guid id, 
        [FromBody] UpdateMedicationIntakeDto dto, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на обновление записи о приеме {IntakeId} пользователя {UserId}", id, userId);
        
        // Проверяем, что запись принадлежит пользователю
        var existingResult = await _intakeService.GetByIdAsync(id, cancellationToken);
        if (!existingResult.IsSuccess)
        {
            return NotFound(new { error = existingResult.Error });
        }

        if (existingResult.Data!.UserId != userId)
        {
            return Forbid();
        }

        var result = await _intakeService.UpdateAsync(id, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Удалить запись о приеме
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на удаление записи о приеме {IntakeId} пользователя {UserId}", id, userId);
        
        // Проверяем, что запись принадлежит пользователю
        var existingResult = await _intakeService.GetByIdAsync(id, cancellationToken);
        if (!existingResult.IsSuccess)
        {
            return NotFound(new { error = existingResult.Error });
        }

        if (existingResult.Data!.UserId != userId)
        {
            return Forbid();
        }

        var result = await _intakeService.DeleteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

