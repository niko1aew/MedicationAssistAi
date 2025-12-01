using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicationAssist.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/[controller]")]
public class MedicationsController : ControllerBase
{
    private readonly IMedicationService _medicationService;
    private readonly ILogger<MedicationsController> _logger;

    public MedicationsController(IMedicationService medicationService, ILogger<MedicationsController> logger)
    {
        _medicationService = medicationService;
        _logger = logger;
    }

    /// <summary>
    /// Получить все лекарства пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicationDto>>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение лекарств пользователя {UserId}", userId);
        var result = await _medicationService.GetByUserIdAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Получить лекарство по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MedicationDto>> GetById(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение лекарства {MedicationId} пользователя {UserId}", id, userId);
        var result = await _medicationService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        // Проверяем, что лекарство принадлежит пользователю
        if (result.Data!.UserId != userId)
        {
            return Forbid();
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Создать новое лекарство
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MedicationDto>> Create(Guid userId, [FromBody] CreateMedicationDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание лекарства для пользователя {UserId}", userId);
        var result = await _medicationService.CreateAsync(userId, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { userId, id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Обновить лекарство
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MedicationDto>> Update(Guid userId, Guid id, [FromBody] UpdateMedicationDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на обновление лекарства {MedicationId} пользователя {UserId}", id, userId);
        
        // Проверяем, что лекарство принадлежит пользователю
        var existingResult = await _medicationService.GetByIdAsync(id, cancellationToken);
        if (!existingResult.IsSuccess)
        {
            return NotFound(new { error = existingResult.Error });
        }

        if (existingResult.Data!.UserId != userId)
        {
            return Forbid();
        }

        var result = await _medicationService.UpdateAsync(id, dto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Удалить лекарство
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на удаление лекарства {MedicationId} пользователя {UserId}", id, userId);
        
        // Проверяем, что лекарство принадлежит пользователю
        var existingResult = await _medicationService.GetByIdAsync(id, cancellationToken);
        if (!existingResult.IsSuccess)
        {
            return NotFound(new { error = existingResult.Error });
        }

        if (existingResult.Data!.UserId != userId)
        {
            return Forbid();
        }

        var result = await _medicationService.DeleteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

