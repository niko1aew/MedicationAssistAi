using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

public interface IMedicationService
{
    Task<Result<MedicationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MedicationDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<MedicationDto>> CreateAsync(Guid userId, CreateMedicationDto dto, CancellationToken cancellationToken = default);
    Task<Result<MedicationDto>> UpdateAsync(Guid id, UpdateMedicationDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

