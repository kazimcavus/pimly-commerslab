using Channels.Application.AttributeChannelMappings.AttributeChannelMappingSupport;
using Channels.Application.AttributeChannelMappings.Catalog;
using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.AttributeChannelMappings.UpsertAttributeValueChannelMappings;

/// <summary>
/// Bir attribute/variant kanal eşlemesi altında Catalog değerleri ile harici pazaryeri değerleri
/// arasında toplu değer eşlemesi oluşturur veya günceller.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Seçim listesi veya sabit değer gerektiren pazaryeri attribute'ları için Catalog
/// değerlerinin harici karşılıklarını tanımlar.</para>
/// <para><b>Ön koşullar:</b> Geçerli üst <see cref="AttributeChannelMapping"/> kaydı, ilgili Catalog
/// kategorisi için tanımlı <see cref="CategoryChannelMapping"/> (kategori eşlemesi zorunludur), geçerli
/// Catalog ve (AllowCustom=false ise) harici değer kayıtları.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → üst alan eşlemesi ve kategori eşlemesi doğrulanır → her
/// girdi için Catalog/harici değer kontrol edilir → eşleme oluşturulur veya güncellenir → zenginleştirilmiş
/// DTO listesi döner.</para>
/// <para><b>Hata durumları:</b> Üst eşleme veya kategori eşlemesi yok, yinelenen Catalog değer id'si,
/// Catalog/harici değer bulunamadı, domain oluşturma hataları.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class UpsertAttributeValueChannelMappingsHandler(
    IValidator<UpsertAttributeValueChannelMappingsCommand> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository fieldMappings,
    IAttributeValueChannelMappingRepository valueMappings,
    IExternalCategoryAttributeRepository externalAttributes,
    IExternalAttributeValueRepository externalValues,
    ICatalogAttributeGateway catalogAttributes,
    ICatalogVariantGateway catalogVariants,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IUpsertAttributeValueChannelMappingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<AttributeValueChannelMappingDto>>> ExecuteAsync(
        UpsertAttributeValueChannelMappingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var parentMapping = await fieldMappings.GetByIdAsync(command.MappingId, cancellationToken);
        if (parentMapping is null
            || parentMapping.Marketplace != marketplace
            || parentMapping.CatalogCategoryId != command.CatalogCategoryId)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                Error.NotFound("Attribute channel mapping not found."));
        }

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            marketplace,
            command.CatalogCategoryId,
            cancellationToken);

        if (externalCategoryId is null)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                Error.NotFound("Category channel mapping required before value mapping."));
        }

        var externalAttribute = await externalAttributes.GetAsync(
            marketplace,
            externalCategoryId,
            parentMapping.ExternalAttributeId,
            cancellationToken);

        if (externalAttribute is null)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                Error.NotFound("External attribute not found."));
        }

        var duplicateCatalogValueIds = command.Values
            .GroupBy(entry => entry.CatalogValueId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateCatalogValueIds.Count > 0)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                Error.Validation("Duplicate catalog value ids are not allowed in the same batch."));
        }

        var upsertedMappings = new List<AttributeValueChannelMapping>(command.Values.Count);

        foreach (var entry in command.Values)
        {
            if (parentMapping.SourceType == AttributeMappingSourceType.CatalogAttribute)
            {
                var catalogValue = await catalogAttributes.GetValueByIdAsync(
                    parentMapping.CatalogSourceId,
                    entry.CatalogValueId,
                    cancellationToken);

                if (catalogValue is null)
                {
                    return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                        Error.NotFound($"Catalog attribute value '{entry.CatalogValueId}' not found."));
                }
            }
            else
            {
                var catalogValue = await catalogVariants.GetValueByIdAsync(
                    parentMapping.CatalogSourceId,
                    entry.CatalogValueId,
                    cancellationToken);

                if (catalogValue is null)
                {
                    return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                        Error.NotFound($"Catalog variant value '{entry.CatalogValueId}' not found."));
                }
            }

            if (!externalAttribute.AllowCustom)
            {
                var externalValue = await externalValues.GetAsync(
                    marketplace,
                    externalCategoryId,
                    parentMapping.ExternalAttributeId,
                    entry.ExternalValueId,
                    cancellationToken);

                if (externalValue is null)
                {
                    return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                        Error.NotFound($"External attribute value '{entry.ExternalValueId}' not found."));
                }
            }

            var existing = await valueMappings.GetAsync(
                parentMapping.Id,
                entry.CatalogValueId,
                cancellationToken);

            if (existing is null)
            {
                var createResult = AttributeValueChannelMapping.Create(
                    tenantContext.TenantId,
                    parentMapping.Id,
                    entry.CatalogValueId,
                    entry.ExternalValueId);

                if (createResult.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(createResult.Error);
                }

                await valueMappings.AddAsync(createResult.Value, cancellationToken);
                upsertedMappings.Add(createResult.Value);
                continue;
            }

            var updateResult = existing.UpdateExternalValue(entry.ExternalValueId);
            if (updateResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(updateResult.Error);
            }

            valueMappings.Update(existing);
            upsertedMappings.Add(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = await AttributeChannelMappingEnricher.EnrichValuesAsync(
            upsertedMappings,
            parentMapping,
            categoryMappings,
            externalValues,
            catalogAttributes,
            catalogVariants,
            cancellationToken);

        return Result.Success(dtos);
    }
}
