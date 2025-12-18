using MedicationAssist.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class LinkTokenConfiguration : IEntityTypeConfiguration<LinkToken>
{
    public void Configure(EntityTypeBuilder<LinkToken> builder)
    {
        builder.ToTable("LinkTokens");

        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(lt => lt.Token)
            .IsUnique();

        builder.Property(lt => lt.UserId)
            .IsRequired();

        builder.Property(lt => lt.ExpiresAt)
            .IsRequired();

        builder.Property(lt => lt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(lt => lt.UsedAt);

        builder.Property(lt => lt.CreatedAt)
            .IsRequired();

        builder.Property(lt => lt.UpdatedAt);

        // Связь с пользователем (только внешний ключ, без навигационного свойства)
        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(lt => lt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы для быстрого поиска
        builder.HasIndex(lt => lt.UserId);
        builder.HasIndex(lt => lt.ExpiresAt);
        builder.HasIndex(lt => new { lt.UserId, lt.IsUsed, lt.ExpiresAt });
    }
}
