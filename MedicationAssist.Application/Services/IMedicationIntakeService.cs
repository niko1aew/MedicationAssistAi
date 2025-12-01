using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

public interface IMedicationIntakeService
{
    Task<Result<MedicationIntakeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MedicationIntakeDto>>> GetByUserIdAsync(
        Guid userId, 
        MedicationIntakeFilterDto? filter = null,
        CancellationToken cancellationToken = default);
    Task<Result<MedicationIntakeDto>> CreateAsync(Guid userId, CreateMedicationIntakeDto dto, CancellationToken cancellationToken = default);
    Task<Result<MedicationIntakeDto>> UpdateAsync(Guid id, UpdateMedicationIntakeDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

