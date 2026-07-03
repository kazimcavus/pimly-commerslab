using Channels.Application.Contracts;
using Channels.Application.Ports;
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

namespace Channels.Application.CategoryChannelMappings.UpsertCategoryChannelMapping;

/// <summary>
/// Catalog kategorisi ile pazaryeri harici kategorisi arasında kanal eşlemesi oluşturur veya günceller.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Bir Catalog kategorisinin hangi pazaryeri yaprak kategorisine karşılık geldiğini
/// tanımlar; attribute eşleme ve ürün yayınlama için temel ön koşuldur.</para>
/// <para><b>Ön koşullar:</b> Aktif pazaryeri, mevcut Catalog kategorisi, cache'te bulunan yaprak
/// (leaf) harici kategori.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → pazaryeri ve Catalog kategorisi doğrulanır → harici kategori
/// cache'ten getirilir ve yaprak olduğu kontrol edilir → mevcut eşleme yoksa oluşturulur, varsa
/// harici id güncellenir → zenginleştirilmiş <see cref="CategoryChannelMappingDto"/> döner.</para>
/// <para><b>Hata durumları:</b> Pasif pazaryeri, Catalog/harici kategori bulunamadı, yaprak olmayan
/// harici kategori (Validation), domain oluşturma/güncelleme hataları.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class UpsertCategoryChannelMappingHandler(
    IValidator<UpsertCategoryChannelMappingCommand> validator,
    ICategoryChannelMappingRepository mappings,
    IExternalCategoryRepository externalCategories,
    ICatalogCategoryGateway catalogCategories,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IUpsertCategoryChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryChannelMappingDto>> ExecuteAsync(
        UpsertCategoryChannelMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoryChannelMappingDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<CategoryChannelMappingDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var catalogCategory = await catalogCategories.GetByIdAsync(command.CatalogCategoryId, cancellationToken);
        if (catalogCategory is null)
        {
            return Result.Failure<CategoryChannelMappingDto>(Error.NotFound("Catalog category not found."));
        }

        var externalCategory = await externalCategories.GetByExternalIdAsync(
            marketplace,
            command.ExternalId,
            cancellationToken);

        if (externalCategory is null)
        {
            return Result.Failure<CategoryChannelMappingDto>(Error.NotFound("External category not found."));
        }

        if (!externalCategory.IsLeaf)
        {
            return Result.Failure<CategoryChannelMappingDto>(
                Error.Validation("Only leaf external categories can be mapped."));
        }

        var existing = await mappings.GetAsync(marketplace, command.CatalogCategoryId, cancellationToken);
        if (existing is null)
        {
            var createResult = CategoryChannelMapping.Create(
                tenantContext.TenantId,
                command.CatalogCategoryId,
                marketplace,
                command.ExternalId);

            if (createResult.IsFailure)
            {
                return Result.Failure<CategoryChannelMappingDto>(createResult.Error);
            }

            await mappings.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(createResult.Value.ToDto(catalogCategory, externalCategory));
        }

        var updateResult = existing.UpdateExternalCategory(command.ExternalId);
        if (updateResult.IsFailure)
        {
            return Result.Failure<CategoryChannelMappingDto>(updateResult.Error);
        }

        mappings.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(existing.ToDto(catalogCategory, externalCategory));
    }
}
