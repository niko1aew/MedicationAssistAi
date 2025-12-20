using MedicationAssist.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class WebLoginTokenConfiguration : IEntityTypeConfiguration<WebLoginToken>
{
    public void Configure(EntityTypeBuilder<WebLoginToken> builder)
    {
        builder.ToTable("WebLoginTokens");

        builder.HasKey(wlt => wlt.Id);

        builder.Property(wlt => wlt.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(wlt => wlt.Token)
            .IsUnique();

        builder.Property(wlt => wlt.UserId)
            .IsRequired();

        builder.Property(wlt => wlt.ExpiresAt)
            .IsRequired();

        builder.Property(wlt => wlt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(wlt => wlt.UsedAt);

        builder.Property(wlt => wlt.CreatedAt)
            .IsRequired();

        builder.Property(wlt => wlt.UpdatedAt);

        // Связь с пользователем (только внешний ключ, без навигационного свойства)
        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(wlt => wlt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы для быстрого поиска
        builder.HasIndex(wlt => wlt.UserId);
        builder.HasIndex(wlt => wlt.ExpiresAt);
        builder.HasIndex(wlt => new { wlt.UserId, wlt.IsUsed, wlt.ExpiresAt });
    }
}
