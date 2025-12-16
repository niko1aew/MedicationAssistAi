using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Domain.Repositories;

/// <summary>
/// Репозиторий напоминаний
/// </summary>
public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reminder>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reminder>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

