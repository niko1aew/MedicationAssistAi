using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicationAssist.Infrastructure.Repositories;

public class MedicationRepository : IMedicationRepository
{
    private readonly ApplicationDbContext _context;

    public MedicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Medication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Medications
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Medications
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Medication> AddAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        await _context.Medications.AddAsync(medication, cancellationToken);
        return medication;
    }

    public Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        _context.Medications.Update(medication);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medication = await _context.Medications.FindAsync(new object[] { id }, cancellationToken);
        if (medication != null)
        {
            _context.Medications.Remove(medication);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Medications.AnyAsync(m => m.Id == id, cancellationToken);
    }
}

