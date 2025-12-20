using MedicationAssist.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationAssist.Infrastructure.Data.Configurations;

public class TelegramLoginTokenConfiguration : IEntityTypeConfiguration<TelegramLoginToken>
{
    public void Configure(EntityTypeBuilder<TelegramLoginToken> builder)
    {
        builder.ToTable("TelegramLoginTokens");

        builder.HasKey(t => t.Token);

        builder.Property(t => t.Token)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.IsAuthorized)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индекс для быстрого поиска истекших токенов
        builder.HasIndex(t => t.ExpiresAt);
    }
}
