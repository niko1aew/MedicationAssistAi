using MedicationAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MedicationAssist.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationIntake> MedicationIntakes => Set<MedicationIntake>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Security.RefreshToken> RefreshTokens => Set<Security.RefreshToken>();
    public DbSet<Security.LinkToken> LinkTokens => Set<Security.LinkToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем все конфигурации из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

