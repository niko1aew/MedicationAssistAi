using MedicationAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class MedicationIntakeConfiguration : IEntityTypeConfiguration<MedicationIntake>
{
    public void Configure(EntityTypeBuilder<MedicationIntake> builder)
    {
        builder.ToTable("MedicationIntakes");

        builder.HasKey(mi => mi.Id);

        builder.Property(mi => mi.UserId)
            .IsRequired();

        builder.Property(mi => mi.MedicationId)
            .IsRequired();

        builder.Property(mi => mi.IntakeTime)
            .IsRequired();

        builder.Property(mi => mi.Notes)
            .HasMaxLength(500);

        builder.Property(mi => mi.CreatedAt)
            .IsRequired();

        builder.Property(mi => mi.UpdatedAt);

        builder.HasIndex(mi => new { mi.UserId, mi.IntakeTime });
        builder.HasIndex(mi => mi.MedicationId);
    }
}

