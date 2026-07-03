using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>TaxonomySyncRun aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class TaxonomySyncRunConfiguration : IEntityTypeConfiguration<TaxonomySyncRun>
{
    public void Configure(EntityTypeBuilder<TaxonomySyncRun> builder)
    {
        builder.ToTable("taxonomy_sync_runs");

        builder.HasKey(syncRun => syncRun.Id);
        builder.Property(syncRun => syncRun.Id).HasColumnName("id");
        builder.Ignore(syncRun => syncRun.DomainEvents);

        builder.Property(syncRun => syncRun.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.HasIndex(syncRun => syncRun.Marketplace);
        builder.HasIndex(syncRun => new { syncRun.Marketplace, syncRun.Status });

        builder.Property(syncRun => syncRun.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToPersistence(status),
                value => FromPersistence(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(syncRun => syncRun.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(syncRun => syncRun.StartedAt)
            .HasColumnName("started_at");

        builder.Property(syncRun => syncRun.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(syncRun => syncRun.ProcessedCount)
            .HasColumnName("processed_count")
            .IsRequired();

        builder.Property(syncRun => syncRun.TotalEstimate)
            .HasColumnName("total_estimate");

        builder.Property(syncRun => syncRun.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);
    }

    private static string ToPersistence(TaxonomySyncStatus status) =>
        status switch
        {
            TaxonomySyncStatus.Pending => "pending",
            TaxonomySyncStatus.Running => "running",
            TaxonomySyncStatus.Completed => "completed",
            TaxonomySyncStatus.Failed => "failed",
            TaxonomySyncStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static TaxonomySyncStatus FromPersistence(string value) =>
        value switch
        {
            "pending" => TaxonomySyncStatus.Pending,
            "running" => TaxonomySyncStatus.Running,
            "completed" => TaxonomySyncStatus.Completed,
            "failed" => TaxonomySyncStatus.Failed,
            "cancelled" => TaxonomySyncStatus.Cancelled,
            _ => TaxonomySyncStatus.Pending,
        };
}
