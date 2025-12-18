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
                _logger.LogWarning("Medication intake record лекарства с ID {IntakeId} not found", id);
                return Result<MedicationIntakeDto>.Failure("Medication intake record not found");
            }

            var medication = await _unitOfWork.Medications.GetByIdAsync(intake.MedicationId, cancellationToken);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication?.Name ?? "Unknown"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting record о приеме {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure($"Error while getting record: {ex.Message}");
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

            var intakeDtos = intakes.Select(i => MapToDto(i, medicationDict.GetValueOrDefault(i.MedicationId, "Unknown")));

            _logger.LogInformation("Retrieved {Count} записей о intakes for user {UserId}", intakeDtos.Count(), userId);
            return Result<IEnumerable<MedicationIntakeDto>>.Success(intakeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while получении записей о intakes for user {UserId}", userId);
            return Result<IEnumerable<MedicationIntakeDto>>.Failure($"Error while получении записей: {ex.Message}");
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
                _logger.LogWarning("Attempt создать запись для несуществующего пользователя {UserId}", userId);
                return Result<MedicationIntakeDto>.Failure("Пользователь не найден");
            }

            var medication = await _unitOfWork.Medications.GetByIdAsync(dto.MedicationId, cancellationToken);
            if (medication == null)
            {
                _logger.LogWarning("Attempt создать запись для несуществующего лекарства {MedicationId}", dto.MedicationId);
                return Result<MedicationIntakeDto>.Failure("Лекарство не найдено");
            }

            if (medication.UserId != userId)
            {
                _logger.LogWarning("Attempt создать запись для лекарства {MedicationId} другого пользователя", dto.MedicationId);
                return Result<MedicationIntakeDto>.Failure("Лекарство belongs to another user");
            }

            // Если время не указано, используем текущее время в часовом поясе пользователя
            DateTime intakeTime;
            if (dto.IntakeTime.HasValue)
            {
                var providedTime = dto.IntakeTime.Value;

                // Если время уже в UTC, используем его как есть
                if (providedTime.Kind == DateTimeKind.Utc)
                {
                    intakeTime = providedTime;
                }
                // Если время локальное или не указано, конвертируем в UTC
                else
                {
                    try
                    {
                        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                        // Для корректной конвертации нужно убедиться, что DateTime имеет Kind = Unspecified
                        var unspecifiedTime = DateTime.SpecifyKind(providedTime, DateTimeKind.Unspecified);
                        intakeTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedTime, userTimeZone);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        _logger.LogWarning("Invalid timezone {TimeZoneId} for user {UserId}, treating as UTC",
                            user.TimeZoneId, userId);
                        intakeTime = providedTime.Kind == DateTimeKind.Local
                            ? providedTime.ToUniversalTime()
                            : DateTime.SpecifyKind(providedTime, DateTimeKind.Utc);
                    }
                }
            }
            else
            {
                intakeTime = DateTime.UtcNow;
            }

            var intake = new MedicationIntake(userId, dto.MedicationId, intakeTime, dto.Notes);

            await _unitOfWork.MedicationIntakes.AddAsync(intake, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created новая запись о приеме {IntakeId} для пользователя {UserId}", intake.Id, userId);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication.Name));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error при creating record о intakes for user {UserId}", userId);
            return Result<MedicationIntakeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating record о intakes for user {UserId}", userId);
            return Result<MedicationIntakeDto>.Failure($"Error while creating record: {ex.Message}");
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
                _logger.LogWarning("Attempt обновить несуществующую запись {IntakeId}", id);
                return Result<MedicationIntakeDto>.Failure("Medication intake record not found");
            }

            intake.SetIntakeTime(dto.IntakeTime);
            intake.SetNotes(dto.Notes);

            await _unitOfWork.MedicationIntakes.UpdateAsync(intake, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var medication = await _unitOfWork.Medications.GetByIdAsync(intake.MedicationId, cancellationToken);

            _logger.LogInformation("Updated запись о приеме {IntakeId}", id);
            return Result<MedicationIntakeDto>.Success(MapToDto(intake, medication?.Name ?? "Unknown"));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error при updating record {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating record {IntakeId}", id);
            return Result<MedicationIntakeDto>.Failure($"Error while updating record: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.MedicationIntakes.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Attempt удалить несуществующую запись {IntakeId}", id);
                return Result.Failure("Medication intake record not found");
            }

            await _unitOfWork.MedicationIntakes.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted запись о приеме {IntakeId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting record {IntakeId}", id);
            return Result.Failure($"Error while deleting record: {ex.Message}");
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

