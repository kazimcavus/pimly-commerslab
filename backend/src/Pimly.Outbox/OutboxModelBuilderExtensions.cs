using Microsoft.EntityFrameworkCore;

namespace Pimly.Outbox;

/// <summary>Outbox tablosunu modülün EF modeline ekler.</summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// <see cref="OutboxMessage"/> eşlemesini kurar. Tablo, çağıran modelin varsayılan şemasında
    /// oluşur — mekanizma ortaktır, tablo modüle aittir.
    /// </summary>
    /// <param name="modelBuilder">Yapılandırılacak model kurucu.</param>
    /// <returns>Zincirleme için aynı model kurucu.</returns>
    public static ModelBuilder AddOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
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
        });

        return modelBuilder;
    }
}
