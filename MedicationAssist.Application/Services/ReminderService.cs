using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Reminder management service (application layer)
/// </summary>
public class ReminderService : IReminderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IUnitOfWork unitOfWork, ILogger<ReminderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ReminderDto>> CreateAsync(
        Guid userId,
        CreateReminderDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempt to create reminder for non-existing user {UserId}", userId);
                return Result<ReminderDto>.Failure("User not found");
            }

            // Проверяем, привязан ли Telegram аккаунт
            if (user.TelegramUserId == null)
            {
                _logger.LogWarning("Attempt to create reminder for user {UserId} without linked Telegram", userId);
                return Result<ReminderDto>.Failure("Telegram account must be linked before creating reminders");
            }

            // Проверяем, что указанный TelegramUserId соответствует привязанному аккаунту
            if (user.TelegramUserId != dto.TelegramUserId)
            {
                _logger.LogWarning("TelegramUserId mismatch for user {UserId}: expected {Expected}, got {Actual}",
                    userId, user.TelegramUserId, dto.TelegramUserId);
                return Result<ReminderDto>.Failure("Specified Telegram ID does not match the linked account");
            }

            var medication = await _unitOfWork.Medications.GetByIdAsync(dto.MedicationId, cancellationToken);
            if (medication == null || medication.UserId != userId)
            {
                _logger.LogWarning("Attempt to create reminder for inaccessible medication {MedicationId}", dto.MedicationId);
                return Result<ReminderDto>.Failure("Medication not found");
            }

            var reminder = new Reminder(userId, dto.TelegramUserId, dto.MedicationId, dto.Time);

            await _unitOfWork.Reminders.AddAsync(reminder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created reminder {ReminderId} for user {UserId}", reminder.Id, userId);
            return Result<ReminderDto>.Success(MapToDto(reminder, medication.Name, medication.Dosage));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating reminder for user {UserId}", userId);
            return Result<ReminderDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating reminder for user {UserId}", userId);
            return Result<ReminderDto>.Failure($"Error while creating reminder: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ReminderDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _unitOfWork.Users.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found when getting reminders", userId);
                return Result<IEnumerable<ReminderDto>>.Failure("User not found");
            }

            var reminders = await _unitOfWork.Reminders.GetByUserIdAsync(userId, cancellationToken);
            var medications = await _unitOfWork.Medications.GetByUserIdAsync(userId, cancellationToken);
            var medicationDict = medications.ToDictionary(m => m.Id, m => m);

            var dtos = reminders.Select(r =>
            {
                medicationDict.TryGetValue(r.MedicationId, out var med);
                return MapToDto(r, med?.Name ?? "Unknown", med?.Dosage);
            });

            return Result<IEnumerable<ReminderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting reminders for user {UserId}", userId);
            return Result<IEnumerable<ReminderDto>>.Failure($"Error while getting reminders: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ReminderDto>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var reminders = await _unitOfWork.Reminders.GetActiveAsync(cancellationToken);

            // Load related medications (cache by Id to avoid extra DB calls)
            var medicationCache = new Dictionary<Guid, Domain.Entities.Medication>();

            foreach (var medId in reminders.Select(r => r.MedicationId).Distinct())
            {
                var med = await _unitOfWork.Medications.GetByIdAsync(medId, cancellationToken);
                if (med != null)
                {
                    medicationCache[medId] = med;
                }
            }

            var dtos = reminders.Select(r =>
            {
                medicationCache.TryGetValue(r.MedicationId, out var med);
                return MapToDto(r, med?.Name ?? "Unknown", med?.Dosage);
            });

            return Result<IEnumerable<ReminderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting active reminders");
            return Result<IEnumerable<ReminderDto>>.Failure($"Error while getting reminders: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Reminders.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Attempt to delete non-existing reminder {ReminderId}", id);
                return Result.Failure("Reminder not found");
            }

            await _unitOfWork.Reminders.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted reminder {ReminderId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting reminder {ReminderId}", id);
            return Result.Failure($"Error while deleting reminder: {ex.Message}");
        }
    }

    public async Task<Result> MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            var reminder = await _unitOfWork.Reminders.GetByIdAsync(id, cancellationToken);
            if (reminder == null)
            {
                _logger.LogWarning("Attempt to mark non-existing reminder {ReminderId} as sent", id);
                return Result.Failure("Reminder not found");
            }

            reminder.MarkSent(sentAtUtc);
            await _unitOfWork.Reminders.UpdateAsync(reminder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while marking reminder {ReminderId} as sent", id);
            return Result.Failure($"Error while updating reminder: {ex.Message}");
        }
    }

    private static ReminderDto MapToDto(Reminder reminder, string medicationName, string? dosage)
    {
        return new ReminderDto(
            reminder.Id,
            reminder.UserId,
            reminder.TelegramUserId,
            reminder.MedicationId,
            medicationName,
            dosage,
            reminder.Time,
            reminder.IsActive,
            reminder.LastSentAt,
            reminder.CreatedAt,
            reminder.UpdatedAt);
    }
}

