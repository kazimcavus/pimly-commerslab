using Channels.Domain.Imports;

namespace Channels.Application.Contracts;

/// <summary>ProductImportRun domain modeli ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class ProductImportMappings
{
    internal static ProductImportRunSummaryDto ToSummaryDto(this ProductImportRun run) =>
        new(
            run.Id,
            run.Marketplace.Code,
            ToStatusString(run.Status),
            run.CreatedAt,
            run.StartedAt,
            run.CompletedAt,
            run.TotalProducts,
            run.ProcessedProducts,
            run.ImportedProducts,
            run.SkippedProducts,
            run.FailedProducts);

    internal static ProductImportRunDto ToDto(this ProductImportRun run) =>
        new(
            run.Id,
            run.Marketplace.Code,
            ToStatusString(run.Status),
            run.CreatedAt,
            run.StartedAt,
            run.CompletedAt,
            run.TotalProducts,
            run.ProcessedProducts,
            run.ImportedProducts,
            run.SkippedProducts,
            run.FailedProducts,
            run.ErrorMessage,
            run.Errors
                .Select(error => new ProductImportErrorDto(error.ProductMainId, error.Barcode, error.Message))
                .ToList());

    internal static string ToStatusString(ProductImportStatus status) =>
        status switch
        {
            ProductImportStatus.Pending => "pending",
            ProductImportStatus.Running => "running",
            ProductImportStatus.Completed => "completed",
            ProductImportStatus.CompletedWithErrors => "completed_with_errors",
            ProductImportStatus.Failed => "failed",
            ProductImportStatus.Cancelled => "cancelled",
            _ => "pending",
        };
}
