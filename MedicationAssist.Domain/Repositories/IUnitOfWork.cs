namespace MedicationAssist.Domain.Repositories;

/// <summary>
/// Единица работы для управления транзакциями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IMedicationRepository Medications { get; }
    IMedicationIntakeRepository MedicationIntakes { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

