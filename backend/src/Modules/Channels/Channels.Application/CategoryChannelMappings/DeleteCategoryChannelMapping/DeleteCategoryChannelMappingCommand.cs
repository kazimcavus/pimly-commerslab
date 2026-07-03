namespace Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;

/// <summary>
/// Kategori kanal eşlemesini silmek için kullanılan komut modeli.
/// </summary>
/// <param name="MarketplaceCode">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemesi silinecek Catalog kategori kimliği.</param>
public sealed record DeleteCategoryChannelMappingCommand(
    string MarketplaceCode,
    Guid CatalogCategoryId);
