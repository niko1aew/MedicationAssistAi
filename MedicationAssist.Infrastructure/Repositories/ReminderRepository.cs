using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicationAssist.Infrastructure.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly ApplicationDbContext _context;

    public ReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Reminder>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Time)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reminder>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.IsActive)
            .OrderBy(r => r.Time)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        await _context.Reminders.AddAsync(reminder, cancellationToken);
        return reminder;
    }

    public Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        _context.Reminders.Update(reminder);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reminder = await _context.Reminders.FindAsync(new object[] { id }, cancellationToken);
        if (reminder != null)
        {
            _context.Reminders.Remove(reminder);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders.AnyAsync(r => r.Id == id, cancellationToken);
    }
}

