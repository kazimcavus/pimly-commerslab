using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Application.Taxonomy.AttributeChannelMappingSupport;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Taxonomy.UpsertAttributeChannelMapping;

/// <summary>
/// Catalog attribute veya variant ile pazaryeri harici attribute alanı arasında kanal eşlemesi oluşturur
/// veya günceller.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Catalog'taki bir attribute/variant'ın pazaryerinde hangi harici alana karşılık
/// geldiğini tanımlar; değer eşleme ve ürün yayınlama için gereklidir.</para>
/// <para><b>Ön koşullar:</b> Aktif pazaryeri, ilgili Catalog kategorisi için tanımlı
/// <see cref="CategoryChannelMapping"/> (kategori eşlemesi zorunludur), geçerli Catalog kaynağı ve
/// cache'teki harici attribute.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → kategori eşlemesinden harici kategori id çözümlenir →
/// Catalog kaynağı ve harici attribute doğrulanır → eşleme oluşturulur veya güncellenir →
/// zenginleştirilmiş <see cref="AttributeChannelMappingDto"/> döner.</para>
/// <para><b>Hata durumları:</b> Kategori eşlemesi yok, Catalog/harici kaynak bulunamadı, attribute
/// kategoriye atanmamış, pasif pazaryeri, geçersiz kaynak tipi.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class UpsertAttributeChannelMappingHandler(
    IValidator<UpsertAttributeChannelMappingCommand> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository mappings,
    IExternalCategoryAttributeRepository externalAttributes,
    ICatalogAttributeGateway catalogAttributes,
    ICatalogVariantGateway catalogVariants,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    ILogger<UpsertAttributeChannelMappingHandler> logger) : IUpsertAttributeChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeChannelMappingDto>> ExecuteAsync(
        UpsertAttributeChannelMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(validationResult.Error);
        }

        var keyResult = MarketplaceKey.Create(command.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(marketplaceResult.Error);
        }

        if (!marketplaceResult.Value.IsActive)
        {
            return Result.Failure<AttributeChannelMappingDto>(Error.Validation("Marketplace is not active."));
        }

        var sourceTypeResult = AttributeMappingSourceTypeParser.Parse(command.SourceType);
        if (sourceTypeResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(sourceTypeResult.Error);
        }

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            keyResult.Value,
            command.CatalogCategoryId,
            cancellationToken);

        if (externalCategoryId is null)
        {
            return Result.Failure<AttributeChannelMappingDto>(
                Error.NotFound("Category channel mapping required before attribute mapping."));
        }

        if (sourceTypeResult.Value == AttributeMappingSourceType.CatalogAttribute)
        {
            var catalogAttribute = await catalogAttributes.GetByIdAsync(command.CatalogSourceId, cancellationToken);
            if (catalogAttribute is null)
            {
                return Result.Failure<AttributeChannelMappingDto>(Error.NotFound("Catalog attribute not found."));
            }

            var belongsToCategory = await catalogAttributes.AttributeBelongsToCategoryAsync(
                command.CatalogCategoryId,
                command.CatalogSourceId,
                cancellationToken);

            if (!belongsToCategory)
            {
                return Result.Failure<AttributeChannelMappingDto>(
                    Error.NotFound("Catalog attribute is not assigned to the category."));
            }
        }
        else
        {
            var catalogVariant = await catalogVariants.GetByIdAsync(command.CatalogSourceId, cancellationToken);
            if (catalogVariant is null)
            {
                return Result.Failure<AttributeChannelMappingDto>(Error.NotFound("Catalog variant not found."));
            }
        }

        var externalAttribute = await externalAttributes.GetAsync(
            keyResult.Value,
            externalCategoryId,
            command.ExternalAttributeId,
            cancellationToken);

        if (externalAttribute is null)
        {
            return Result.Failure<AttributeChannelMappingDto>(Error.NotFound("External attribute not found."));
        }

        if (sourceTypeResult.Value == AttributeMappingSourceType.CatalogVariant && !externalAttribute.IsVariant)
        {
            logger.LogWarning(
                "Variant source {CatalogSourceId} mapped to non-variant external attribute {ExternalAttributeId} for category {CatalogCategoryId}.",
                command.CatalogSourceId,
                command.ExternalAttributeId,
                command.CatalogCategoryId);
        }

        var existing = await mappings.GetAsync(
            keyResult.Value,
            command.CatalogCategoryId,
            sourceTypeResult.Value,
            command.CatalogSourceId,
            cancellationToken);

        if (existing is null)
        {
            var createResult = AttributeChannelMapping.Create(
                tenantContext.TenantId,
                keyResult.Value,
                command.CatalogCategoryId,
                sourceTypeResult.Value,
                command.CatalogSourceId,
                command.ExternalAttributeId);

            if (createResult.IsFailure)
            {
                return Result.Failure<AttributeChannelMappingDto>(createResult.Error);
            }

            await mappings.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var createdDto = await AttributeChannelMappingEnricher.EnrichAsync(
                createResult.Value,
                categoryMappings,
                externalAttributes,
                catalogAttributes,
                catalogVariants,
                cancellationToken);

            return Result.Success(createdDto);
        }

        var updateResult = existing.UpdateExternalAttribute(command.ExternalAttributeId);
        if (updateResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(updateResult.Error);
        }

        mappings.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedDto = await AttributeChannelMappingEnricher.EnrichAsync(
            existing,
            categoryMappings,
            externalAttributes,
            catalogAttributes,
            catalogVariants,
            cancellationToken);

        return Result.Success(updatedDto);
    }
}
