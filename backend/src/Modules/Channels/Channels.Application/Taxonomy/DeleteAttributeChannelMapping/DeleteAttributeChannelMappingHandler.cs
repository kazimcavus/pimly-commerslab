using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.DeleteAttributeChannelMapping;

/// <summary>
/// Belirli bir attribute/variant kanal eşlemesini ve altındaki tüm değer eşlemelerini kalıcı olarak siler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Artık geçerli olmayan alan eşlemelerini kaldırır; bağlı
/// <see cref="AttributeValueChannelMapping"/> kayıtları da temizlenir.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri, Catalog kategori ve mevcut
/// <see cref="AttributeChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → eşleme getirilir ve bağlam kontrol edilir → alt değer
/// eşlemeleri silinir → alan eşlemesi silinir ve kaydedilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, eşleme bulunamadı veya bağlam uyuşmazlığı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class DeleteAttributeChannelMappingHandler(
    IValidator<DeleteAttributeChannelMappingCommand> validator,
    IAttributeChannelMappingRepository mappings,
    IAttributeValueChannelMappingRepository valueMappings,
    IUnitOfWork unitOfWork) : IDeleteAttributeChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteAttributeChannelMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var keyResult = MarketplaceKey.Create(command.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure(marketplaceResult.Error);
        }

        var mapping = await mappings.GetByIdAsync(command.MappingId, cancellationToken);
        if (mapping is null
            || mapping.MarketplaceKey != keyResult.Value
            || mapping.CatalogCategoryId != command.CatalogCategoryId)
        {
            return Result.Failure(Error.NotFound("Attribute channel mapping not found."));
        }

        await valueMappings.RemoveByFieldMappingAsync(mapping.Id, cancellationToken);
        mappings.Remove(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
