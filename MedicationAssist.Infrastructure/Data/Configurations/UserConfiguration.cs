using MedicationAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.TelegramUserId)
            .IsRequired(false);

        builder.HasIndex(u => u.TelegramUserId);

        builder.Property(u => u.TelegramUsername)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(u => u.TimeZoneId)
            .IsRequired()
            .HasDefaultValue("Europe/Moscow");

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        // Связи
        builder.HasMany(u => u.Medications)
            .WithOne()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MedicationIntakes)
            .WithOne()
            .HasForeignKey(mi => mi.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

