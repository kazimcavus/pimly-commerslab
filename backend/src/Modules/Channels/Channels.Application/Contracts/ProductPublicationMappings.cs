using Channels.Domain.Publications;

namespace Channels.Application.Contracts;

/// <summary>ProductPublicationRun domain modeli ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class ProductPublicationMappings
{
    internal static ProductPublicationRunSummaryDto ToSummaryDto(this ProductPublicationRun run) =>
        new(
            run.Id,
            run.Marketplace.Code,
            ToStatusString(run.Status),
            run.CreatedAt,
            run.StartedAt,
            run.CompletedAt,
            run.TotalItems,
            run.ProcessedItems,
            run.PublishedItems,
            run.FailedItems);

    internal static ProductPublicationRunDto ToDto(this ProductPublicationRun run) =>
        new(
            run.Id,
            run.Marketplace.Code,
            ToStatusString(run.Status),
            run.CreatedAt,
            run.StartedAt,
            run.CompletedAt,
            run.TotalItems,
            run.ProcessedItems,
            run.PublishedItems,
            run.FailedItems,
            run.ErrorMessage,
            run.Errors
                .Select(error => new ProductPublicationErrorDto(error.ProductItemId, error.Message))
                .ToList());

    internal static string ToStatusString(PublicationStatus status) =>
        status switch
        {
            PublicationStatus.Pending => "pending",
            PublicationStatus.Running => "running",
            PublicationStatus.Completed => "completed",
            PublicationStatus.CompletedWithErrors => "completed_with_errors",
            PublicationStatus.Failed => "failed",
            _ => "pending",
        };
}
