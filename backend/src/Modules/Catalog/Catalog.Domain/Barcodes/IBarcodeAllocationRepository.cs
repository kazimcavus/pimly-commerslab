using Catalog.Domain.Barcodes;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>Barkod tahsis kayıtlarının kalıcılık işlemlerini tanımlar.</summary>
public interface IBarcodeAllocationRepository
{
    /// <summary>Belirtilen barkodun daha önce tahsis edilip edilmediğini döner.</summary>
    /// <param name="barcode">Sorgulanacak barkod değeri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<bool> ExistsAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Tahsis edilmiş sayısal barkodların en yüksek değerini döner; kayıt yoksa 0.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<long> MaxNumericBarcodeAsync(CancellationToken cancellationToken = default);

    /// <summary>Birden fazla tahsis kaydını kalıcı depoya ekler.</summary>
    /// <param name="allocations">Eklenecek tahsis kayıtları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddRangeAsync(
        IEnumerable<BarcodeAllocation> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>Tahsis kayıtlarını tahsis zamanına göre azalan sırada sayfalar.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<BarcodeAllocation>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default);
}
