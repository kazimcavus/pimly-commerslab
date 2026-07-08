using Catalog.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>OutboxMessage varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(m => m.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(m => m.Error).HasColumnName("error");

        // Dağıtıcı yalnızca işlenmemiş kayıtları sıralı çeker.
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc });
    }
}
