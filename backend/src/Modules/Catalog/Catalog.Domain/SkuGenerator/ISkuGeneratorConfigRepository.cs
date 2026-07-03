namespace Catalog.Domain.SkuGenerator;

/// <summary>SKU oluşturucu yapılandırması kalıcılık sözleşmesi.</summary>
public interface ISkuGeneratorConfigRepository
{
    /// <summary>Tenant SKU oluşturucu yapılandırmasını getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<SkuGeneratorConfig?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>İlk SKU oluşturucu yapılandırmasını kalıcı depoya ekler.</summary>
    /// <param name="config">Eklenecek yapılandırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(SkuGeneratorConfig config, CancellationToken cancellationToken = default);

    /// <summary>SKU oluşturucu yapılandırmasındaki değişiklikleri izlemeye alır.</summary>
    /// <param name="config">Güncellenmiş yapılandırma.</param>
    void Update(SkuGeneratorConfig config);
}
