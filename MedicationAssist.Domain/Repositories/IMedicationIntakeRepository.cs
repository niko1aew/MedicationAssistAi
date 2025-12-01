using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Domain.Repositories;

/// <summary>
/// Репозиторий записей о приеме лекарств
/// </summary>
public interface IMedicationIntakeRepository
{
    Task<MedicationIntake?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicationIntake>> GetByUserIdAsync(
        Guid userId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default);
    Task<MedicationIntake> AddAsync(MedicationIntake intake, CancellationToken cancellationToken = default);
    Task UpdateAsync(MedicationIntake intake, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

