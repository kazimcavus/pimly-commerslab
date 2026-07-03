using Channels.Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>ProductImportRun aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class ProductImportRunConfiguration : IEntityTypeConfiguration<ProductImportRun>
{
    public void Configure(EntityTypeBuilder<ProductImportRun> builder)
    {
        builder.ToTable("product_import_runs");

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
        builder.Property(run => run.TotalProducts).HasColumnName("total_products");
        builder.Property(run => run.ProcessedProducts).HasColumnName("processed_products").IsRequired();
        builder.Property(run => run.ImportedProducts).HasColumnName("imported_products").IsRequired();
        builder.Property(run => run.SkippedProducts).HasColumnName("skipped_products").IsRequired();
        builder.Property(run => run.FailedProducts).HasColumnName("failed_products").IsRequired();
        builder.Property(run => run.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);

        builder.HasIndex(run => new { run.Status, run.CreatedAt });
        builder.HasIndex(run => new { run.TenantId, run.Marketplace, run.CreatedAt }).IsDescending(false, false, true);

        builder.OwnsMany(run => run.Errors, errors =>
        {
            errors.ToTable("product_import_run_errors");
            errors.WithOwner().HasForeignKey("product_import_run_id");
            errors.HasKey(error => error.Id);
            errors.Property(error => error.Id).HasColumnName("id");
            errors.Property(error => error.ProductMainId)
                .HasColumnName("product_main_id")
                .HasMaxLength(200)
                .IsRequired();
            errors.Property(error => error.Barcode)
                .HasColumnName("barcode")
                .HasMaxLength(200);
            errors.Property(error => error.Message)
                .HasColumnName("message")
                .HasMaxLength(ProductImportError.MessageMaxLength)
                .IsRequired();
            errors.Ignore(error => error.DomainEvents);
        });

        builder.Navigation(run => run.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static string ToPersistence(ProductImportStatus status) =>
        status switch
        {
            ProductImportStatus.Pending => "pending",
            ProductImportStatus.Running => "running",
            ProductImportStatus.Completed => "completed",
            ProductImportStatus.CompletedWithErrors => "completed_with_errors",
            ProductImportStatus.Failed => "failed",
            ProductImportStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static ProductImportStatus FromPersistence(string value) =>
        value switch
        {
            "pending" => ProductImportStatus.Pending,
            "running" => ProductImportStatus.Running,
            "completed" => ProductImportStatus.Completed,
            "completed_with_errors" => ProductImportStatus.CompletedWithErrors,
            "failed" => ProductImportStatus.Failed,
            "cancelled" => ProductImportStatus.Cancelled,
            _ => ProductImportStatus.Pending,
        };
}
