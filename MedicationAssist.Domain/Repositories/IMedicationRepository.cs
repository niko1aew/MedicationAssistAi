using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Domain.Repositories;

/// <summary>
/// Репозиторий лекарств
/// </summary>
public interface IMedicationRepository
{
    Task<Medication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Medication>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Medication> AddAsync(Medication medication, CancellationToken cancellationToken = default);
    Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

