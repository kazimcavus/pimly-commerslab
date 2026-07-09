using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;

/// <summary>
/// Belirli bir Catalog kategorisi için tanımlı kategori kanal eşlemesini kalıcı olarak siler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Artık geçerli olmayan veya yanlış tanımlanmış kategori eşlemelerini kaldırır.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı ve mevcut bir
/// <see cref="CategoryChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → pazaryeri çözümlenir → eşleme getirilir → depodan silinir
/// ve kaydedilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri, eşleme bulunamadı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class DeleteCategoryChannelMappingHandler(
    IValidator<DeleteCategoryChannelMappingCommand> validator,
    ICategoryChannelMappingRepository mappings,
    IUnitOfWork unitOfWork) : IDeleteCategoryChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteCategoryChannelMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var mapping = await mappings.GetAsync(marketplace, command.CatalogCategoryId, cancellationToken);
        if (mapping is null)
        {
            return Result.Failure(Error.NotFound("Category channel mapping not found."));
        }

        mappings.Remove(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
