using SharedKernel;

namespace Channels.Domain.ExternalCatalog;

/// <summary>ExternalAttributeValue cache depo arabirimi.</summary>
public interface IExternalAttributeValueRepository
{
    Task<ExternalAttributeValue?> GetAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalAttributeValue>> ListByAttributeAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalAttributeValue>> ListByCategoryAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default);
}
