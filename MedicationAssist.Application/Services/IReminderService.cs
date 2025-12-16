using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

public interface IReminderService
{
    Task<Result<ReminderDto>> CreateAsync(Guid userId, CreateReminderDto dto, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ReminderDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ReminderDto>>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default);
}

