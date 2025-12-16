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
                _logger.LogWarning("Medication with ID {MedicationId} not found", id);
                return Result<MedicationDto>.Failure("Medication not found");
            }

            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting medication {MedicationId}", id);
            return Result<MedicationDto>.Failure($"Error while getting medication: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<MedicationDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _unitOfWork.Users.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return Result<IEnumerable<MedicationDto>>.Failure("User not found");
            }

            var medications = await _unitOfWork.Medications.GetByUserIdAsync(userId, cancellationToken);
            var medicationDtos = medications.Select(MapToDto);

            _logger.LogInformation("Retrieved {Count} medications for user {UserId}", medicationDtos.Count(), userId);
            return Result<IEnumerable<MedicationDto>>.Success(medicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting medication list for user {UserId}", userId);
            return Result<IEnumerable<MedicationDto>>.Failure($"Error while getting medication list: {ex.Message}");
        }
    }

    public async Task<Result<MedicationDto>> CreateAsync(Guid userId, CreateMedicationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempt to create medication for non-existent user {UserId}", userId);
                return Result<MedicationDto>.Failure("User not found");
            }

            var medication = new Medication(userId, dto.Name, dto.Description, dto.Dosage);
            await _unitOfWork.Medications.AddAsync(medication, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new medication {MedicationId} for user {UserId}", medication.Id, userId);
            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating medication for user {UserId}", userId);
            return Result<MedicationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating medication for user {UserId}", userId);
            return Result<MedicationDto>.Failure($"Error while creating medication: {ex.Message}");
        }
    }

    public async Task<Result<MedicationDto>> UpdateAsync(Guid id, UpdateMedicationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(id, cancellationToken);
            if (medication == null)
            {
                _logger.LogWarning("Attempt to update non-existent medication {MedicationId}", id);
                return Result<MedicationDto>.Failure("Medication not found");
            }

            medication.SetName(dto.Name);
            medication.SetDescription(dto.Description);
            medication.SetDosage(dto.Dosage);

            await _unitOfWork.Medications.UpdateAsync(medication, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated medication {MedicationId}", id);
            return Result<MedicationDto>.Success(MapToDto(medication));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating medication {MedicationId}", id);
            return Result<MedicationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating medication {MedicationId}", id);
            return Result<MedicationDto>.Failure($"Error while updating medication: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Medications.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Attempt to delete non-existent medication {MedicationId}", id);
                return Result.Failure("Medication not found");
            }

            await _unitOfWork.Medications.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted medication {MedicationId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting medication {MedicationId}", id);
            return Result.Failure($"Error while deleting medication: {ex.Message}");
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

