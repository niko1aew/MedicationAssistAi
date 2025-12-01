using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Application.Services;

public class MedicationService : IMedicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicationService> _logger;

    public MedicationService(IUnitOfWork unitOfWork, ILogger<MedicationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<MedicationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(id, cancellationToken);
            if (medication == null)
            {
                _logger.LogWarning("Лекарство с ID {MedicationId} не найдено", id);
                return Result<MedicationDto>.Failure("Лекарство не найдено");
            }

            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении лекарства {MedicationId}", id);
            return Result<MedicationDto>.Failure($"Ошибка при получении лекарства: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<MedicationDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _unitOfWork.Users.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден", userId);
                return Result<IEnumerable<MedicationDto>>.Failure("Пользователь не найден");
            }

            var medications = await _unitOfWork.Medications.GetByUserIdAsync(userId, cancellationToken);
            var medicationDtos = medications.Select(MapToDto);

            _logger.LogInformation("Получено {Count} лекарств для пользователя {UserId}", medicationDtos.Count(), userId);
            return Result<IEnumerable<MedicationDto>>.Success(medicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка лекарств для пользователя {UserId}", userId);
            return Result<IEnumerable<MedicationDto>>.Failure($"Ошибка при получении списка лекарств: {ex.Message}");
        }
    }

    public async Task<Result<MedicationDto>> CreateAsync(Guid userId, CreateMedicationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Попытка создать лекарство для несуществующего пользователя {UserId}", userId);
                return Result<MedicationDto>.Failure("Пользователь не найден");
            }

            var medication = new Medication(userId, dto.Name, dto.Description, dto.Dosage);
            await _unitOfWork.Medications.AddAsync(medication, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Создано новое лекарство {MedicationId} для пользователя {UserId}", medication.Id, userId);
            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при создании лекарства для пользователя {UserId}", userId);
            return Result<MedicationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании лекарства для пользователя {UserId}", userId);
            return Result<MedicationDto>.Failure($"Ошибка при создании лекарства: {ex.Message}");
        }
    }

    public async Task<Result<MedicationDto>> UpdateAsync(Guid id, UpdateMedicationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(id, cancellationToken);
            if (medication == null)
            {
                _logger.LogWarning("Попытка обновить несуществующее лекарство {MedicationId}", id);
                return Result<MedicationDto>.Failure("Лекарство не найдено");
            }

            medication.SetName(dto.Name);
            medication.SetDescription(dto.Description);
            medication.SetDosage(dto.Dosage);

            await _unitOfWork.Medications.UpdateAsync(medication, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Обновлено лекарство {MedicationId}", id);
            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при обновлении лекарства {MedicationId}", id);
            return Result<MedicationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении лекарства {MedicationId}", id);
            return Result<MedicationDto>.Failure($"Ошибка при обновлении лекарства: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Medications.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Попытка удалить несуществующее лекарство {MedicationId}", id);
                return Result.Failure("Лекарство не найдено");
            }

            await _unitOfWork.Medications.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Удалено лекарство {MedicationId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении лекарства {MedicationId}", id);
            return Result.Failure($"Ошибка при удалении лекарства: {ex.Message}");
        }
    }

    private static MedicationDto MapToDto(Medication medication)
    {
        return new MedicationDto(
            medication.Id,
            medication.UserId,
            medication.Name,
            medication.Description,
            medication.Dosage,
            medication.CreatedAt,
            medication.UpdatedAt
        );
    }
}

