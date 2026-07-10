using Channels.Domain.Publications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>ProductPublicationRun aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class ProductPublicationRunConfiguration : IEntityTypeConfiguration<ProductPublicationRun>
{
    public void Configure(EntityTypeBuilder<ProductPublicationRun> builder)
    {
        builder.ToTable("product_publication_runs");

        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).HasColumnName("id");
        builder.Ignore(run => run.DomainEvents);

        builder.Property(run => run.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(run => run.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.Property(run => run.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToPersistence(status),
                value => FromPersistence(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(run => run.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(run => run.StartedAt).HasColumnName("started_at");
        builder.Property(run => run.CompletedAt).HasColumnName("completed_at");
        builder.Property(run => run.TotalItems).HasColumnName("total_items");
        builder.Property(run => run.ProcessedItems).HasColumnName("processed_items").IsRequired();
        builder.Property(run => run.PublishedItems).HasColumnName("published_items").IsRequired();
        builder.Property(run => run.FailedItems).HasColumnName("failed_items").IsRequired();
        builder.Property(run => run.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);

        builder.HasIndex(run => new { run.Status, run.CreatedAt });
        builder.HasIndex(run => new { run.TenantId, run.Marketplace, run.CreatedAt }).IsDescending(false, false, true);

        builder.OwnsMany(run => run.Errors, errors =>
        {
            errors.ToTable("product_publication_run_errors");
            errors.WithOwner().HasForeignKey("product_publication_run_id");
            errors.HasKey(error => error.Id);
            errors.Property(error => error.Id).HasColumnName("id");
            errors.Property(error => error.ProductItemId)
                .HasColumnName("product_item_id")
                .IsRequired();
            errors.Property(error => error.Message)
                .HasColumnName("message")
                .HasMaxLength(ProductPublicationError.MessageMaxLength)
                .IsRequired();
            errors.Ignore(error => error.DomainEvents);
        });

        builder.Navigation(run => run.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static string ToPersistence(PublicationStatus status) =>
        status switch
        {
            PublicationStatus.Pending => "pending",
            PublicationStatus.Running => "running",
            PublicationStatus.Completed => "completed",
            PublicationStatus.CompletedWithErrors => "completed_with_errors",
            PublicationStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static PublicationStatus FromPersistence(string value) =>
        value switch
        {
            "pending" => PublicationStatus.Pending,
            "running" => PublicationStatus.Running,
            "completed" => PublicationStatus.Completed,
            "completed_with_errors" => PublicationStatus.CompletedWithErrors,
            "failed" => PublicationStatus.Failed,
            _ => PublicationStatus.Pending,
        };
}
