using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.DeleteAttributeValueChannelMapping;

/// <summary>
/// Belirli bir attribute/variant kanal eşlemesi altındaki tek bir değer eşlemesini kalıcı olarak siler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Artık geçerli olmayan Catalog-harici değer eşlemelerini kaldırır.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri, Catalog kategori, üst
/// <see cref="AttributeChannelMapping"/> ve silinecek
/// <see cref="AttributeValueChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → üst alan eşlemesi doğrulanır → değer eşlemesi getirilir
/// ve üst eşleme ile ilişkisi kontrol edilir → silinir ve kaydedilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, üst veya değer eşlemesi bulunamadı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class DeleteAttributeValueChannelMappingHandler(
    IValidator<DeleteAttributeValueChannelMappingCommand> validator,
    IAttributeChannelMappingRepository fieldMappings,
    IAttributeValueChannelMappingRepository valueMappings,
    IUnitOfWork unitOfWork) : IDeleteAttributeValueChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteAttributeValueChannelMappingCommand command,
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

        var parentMapping = await fieldMappings.GetByIdAsync(command.MappingId, cancellationToken);
        if (parentMapping is null
            || parentMapping.MarketplaceKey != keyResult.Value
            || parentMapping.CatalogCategoryId != command.CatalogCategoryId)
        {
            return Result.Failure(Error.NotFound("Attribute channel mapping not found."));
        }

        var valueMapping = await valueMappings.GetByIdAsync(command.ValueMappingId, cancellationToken);
        if (valueMapping is null || valueMapping.AttributeChannelMappingId != parentMapping.Id)
        {
            return Result.Failure(Error.NotFound("Attribute value channel mapping not found."));
        }

        valueMappings.Remove(valueMapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
