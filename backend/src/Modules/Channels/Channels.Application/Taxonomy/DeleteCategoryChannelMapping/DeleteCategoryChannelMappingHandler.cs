using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.DeleteCategoryChannelMapping;

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

        var mapping = await mappings.GetAsync(keyResult.Value, command.CatalogCategoryId, cancellationToken);
        if (mapping is null)
        {
            return Result.Failure(Error.NotFound("Category channel mapping not found."));
        }

        mappings.Remove(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
