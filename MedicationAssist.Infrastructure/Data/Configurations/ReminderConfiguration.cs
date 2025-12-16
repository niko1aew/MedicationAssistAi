using MedicationAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.TelegramUserId)
            .IsRequired();

        builder.Property(r => r.MedicationId)
            .IsRequired();

        builder.Property(r => r.Time)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.LastSentAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        builder.HasIndex(r => new { r.UserId, r.Time });
        builder.HasIndex(r => r.TelegramUserId);
        builder.HasIndex(r => r.MedicationId);

        builder.HasOne<User>()
            .WithMany(u => u.Reminders)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Medication>()
            .WithMany()
            .HasForeignKey(r => r.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

