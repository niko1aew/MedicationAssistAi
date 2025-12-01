using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Application.Services;

public class MedicationIntakeService : IMedicationIntakeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicationIntakeService> _logger;

    public MedicationIntakeService(IUnitOfWork unitOfWork, ILogger<MedicationIntakeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<MedicationIntakeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var intake = await _unitOfWork.MedicationIntakes.GetByIdAsync(id, cancellationToken);
            if (intake == null)
            {
                _logger.LogWarning("Запись о приеме лекарства с ID {IntakeId} не найдена", id);
                return Result<MedicationIntakeDto>.Failure("Запись о приеме не найдена");
            }

            var medication = await _unitOfWork.Medications.GetByIdAsync(intake.MedicationId, cancellationToken);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication?.Name ?? "Неизвестно"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении записи о приеме {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure($"Ошибка при получении записи: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<MedicationIntakeDto>>> GetByUserIdAsync(
        Guid userId, 
        MedicationIntakeFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _unitOfWork.Users.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден", userId);
                return Result<IEnumerable<MedicationIntakeDto>>.Failure("Пользователь не найден");
            }

            var intakes = await _unitOfWork.MedicationIntakes.GetByUserIdAsync(
                userId,
                filter?.FromDate,
                filter?.ToDate,
                filter?.MedicationId,
                cancellationToken);

            var medications = await _unitOfWork.Medications.GetByUserIdAsync(userId, cancellationToken);
            var medicationDict = medications.ToDictionary(m => m.Id, m => m.Name);

            var intakeDtos = intakes.Select(i => MapToDto(i, medicationDict.GetValueOrDefault(i.MedicationId, "Неизвестно")));

            _logger.LogInformation("Получено {Count} записей о приеме для пользователя {UserId}", intakeDtos.Count(), userId);
            return Result<IEnumerable<MedicationIntakeDto>>.Success(intakeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении записей о приеме для пользователя {UserId}", userId);
            return Result<IEnumerable<MedicationIntakeDto>>.Failure($"Ошибка при получении записей: {ex.Message}");
        }
    }

    public async Task<Result<MedicationIntakeDto>> CreateAsync(
        Guid userId, 
        CreateMedicationIntakeDto dto, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Попытка создать запись для несуществующего пользователя {UserId}", userId);
                return Result<MedicationIntakeDto>.Failure("Пользователь не найден");
            }

            var medication = await _unitOfWork.Medications.GetByIdAsync(dto.MedicationId, cancellationToken);
            if (medication == null)
            {
                _logger.LogWarning("Попытка создать запись для несуществующего лекарства {MedicationId}", dto.MedicationId);
                return Result<MedicationIntakeDto>.Failure("Лекарство не найдено");
            }

            if (medication.UserId != userId)
            {
                _logger.LogWarning("Попытка создать запись для лекарства {MedicationId} другого пользователя", dto.MedicationId);
                return Result<MedicationIntakeDto>.Failure("Лекарство принадлежит другому пользователю");
            }

            var intakeTime = dto.IntakeTime ?? DateTime.UtcNow;
            var intake = new MedicationIntake(userId, dto.MedicationId, intakeTime, dto.Notes);

            await _unitOfWork.MedicationIntakes.AddAsync(intake, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Создана новая запись о приеме {IntakeId} для пользователя {UserId}", intake.Id, userId);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication.Name));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при создании записи о приеме для пользователя {UserId}", userId);
            return Result<MedicationIntakeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании записи о приеме для пользователя {UserId}", userId);
            return Result<MedicationIntakeDto>.Failure($"Ошибка при создании записи: {ex.Message}");
        }
    }

    public async Task<Result<MedicationIntakeDto>> UpdateAsync(
        Guid id, 
        UpdateMedicationIntakeDto dto, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var intake = await _unitOfWork.MedicationIntakes.GetByIdAsync(id, cancellationToken);
            if (intake == null)
            {
                _logger.LogWarning("Попытка обновить несуществующую запись {IntakeId}", id);
                return Result<MedicationIntakeDto>.Failure("Запись о приеме не найдена");
            }

            intake.SetIntakeTime(dto.IntakeTime);
            intake.SetNotes(dto.Notes);

            await _unitOfWork.MedicationIntakes.UpdateAsync(intake, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var medication = await _unitOfWork.Medications.GetByIdAsync(intake.MedicationId, cancellationToken);

            _logger.LogInformation("Обновлена запись о приеме {IntakeId}", id);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication?.Name ?? "Неизвестно"));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при обновлении записи {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении записи {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure($"Ошибка при обновлении записи: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.MedicationIntakes.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Попытка удалить несуществующую запись {IntakeId}", id);
                return Result.Failure("Запись о приеме не найдена");
            }

            await _unitOfWork.MedicationIntakes.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Удалена запись о приеме {IntakeId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении записи {IntakeId}", id);
            return Result.Failure($"Ошибка при удалении записи: {ex.Message}");
        }
    }

    private static MedicationIntakeDto MapToDto(MedicationIntake intake, string medicationName)
    {
        return new MedicationIntakeDto(
            intake.Id,
            intake.UserId,
            intake.MedicationId,
            medicationName,
            intake.IntakeTime,
            intake.Notes,
            intake.CreatedAt,
            intake.UpdatedAt
        );
    }
}

