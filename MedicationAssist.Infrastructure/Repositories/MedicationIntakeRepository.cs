using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicationAssist.Infrastructure.Repositories;

public class MedicationIntakeRepository : IMedicationIntakeRepository
{
    private readonly ApplicationDbContext _context;

    public MedicationIntakeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicationIntake?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MedicationIntakes
            .FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<MedicationIntake>> GetByUserIdAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MedicationIntakes
            .Where(mi => mi.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(mi => mi.IntakeTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(mi => mi.IntakeTime <= toDate.Value);
        }

        if (medicationId.HasValue)
        {
            query = query.Where(mi => mi.MedicationId == medicationId.Value);
        }

        return await query
            .OrderByDescending(mi => mi.IntakeTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicationIntake> AddAsync(MedicationIntake intake, CancellationToken cancellationToken = default)
    {
        await _context.MedicationIntakes.AddAsync(intake, cancellationToken);
        return intake;
    }

    public Task UpdateAsync(MedicationIntake intake, CancellationToken cancellationToken = default)
    {
        _context.MedicationIntakes.Update(intake);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var intake = await _context.MedicationIntakes.FindAsync(new object[] { id }, cancellationToken);
        if (intake != null)
        {
            _context.MedicationIntakes.Remove(intake);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MedicationIntakes.AnyAsync(mi => mi.Id == id, cancellationToken);
    }
}

