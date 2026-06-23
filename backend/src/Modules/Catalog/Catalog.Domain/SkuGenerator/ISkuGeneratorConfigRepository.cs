namespace Catalog.Domain.SkuGenerator;

/// <summary>SKU oluşturucu yapılandırması kalıcılık sözleşmesi.</summary>
public interface ISkuGeneratorConfigRepository
{
    Task<SkuGeneratorConfig?> GetAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SkuGeneratorConfig config, CancellationToken cancellationToken = default);

    void Update(SkuGeneratorConfig config);
}
