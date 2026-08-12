using Channels.Domain;
using Channels.Domain.Listings;
using SharedKernel;

namespace Channels.Application.Listings.MarkListingsDirty;

/// <summary>
/// Değişen kalemin listelemelerini kirli işaretler. Pazaryerine <em>çağrı yapmaz</em> — yalnızca bayrak
/// koyar; gerçek gönderim senkron worker'ı tarafından toplu ve debounce edilmiş şekilde yapılır.
/// </summary>
/// <remarks>
/// <para><b>Neden push değil işaretleme:</b> Toplu fiyat güncellemesi veya yeniden import binlerce olay
/// üretir. Her olayda HTTP çağırmak rate limit'e çarpar ve çağrıyı DB transaction'ının dibine koyar.
/// İşaretleme idempotent bir set işlemi olduğu için aynı kalemin bir dakikadaki 100 değişimi tek
/// gönderime iner.</para>
/// <para><b>Hiç listelenmemiş kalemler:</b> Kayıt bulunmazsa sessizce geçilir — pazaryerinde karşılığı
/// olmayan kalem için yapılacak bir şey yoktur.</para>
/// </remarks>
public sealed class MarkListingsDirtyHandler(
    IProductListingRepository listings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IMarkListingsDirtyHandler
{
    /// <inheritdoc/>
    public async Task<Result<int>> ExecuteAsync(
        MarkListingsDirtyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ProductItemId == Guid.Empty)
        {
            return Result.Failure<int>(Error.Validation("Product item id is required."));
        }

        Marketplace? marketplace = null;
        if (command.MarketplaceCode is not null)
        {
            var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
            if (marketplaceResult.IsFailure)
            {
                return Result.Failure<int>(marketplaceResult.Error);
            }

            marketplace = marketplaceResult.Value;
        }

        var candidates = await listings.ListByProductItemAsync(command.ProductItemId, cancellationToken);
        if (candidates.Count == 0)
        {
            return Result.Success(0);
        }

        var now = timeProvider.GetUtcNow();
        var marked = 0;

        foreach (var listing in candidates)
        {
            if (marketplace is not null && listing.Marketplace != marketplace)
            {
                continue;
            }

            if (command.Kind is ListingDirtyKind.Offer or ListingDirtyKind.Both)
            {
                listing.MarkOfferDirty(now);
            }

            if (command.Kind is ListingDirtyKind.Content or ListingDirtyKind.Both)
            {
                listing.MarkContentDirty(now);
            }

            listings.Update(listing);
            marked++;
        }

        if (marked > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(marked);
    }
}
