using SharedKernel;

namespace Channels.Domain.ProductImports;

/// <summary>Ürün import'u sırasında tek bir ürün grubu için oluşan hata kaydı.</summary>
public sealed class ProductImportError : Entity<Guid>
{
    /// <summary>Hata mesajının izin verilen en büyük uzunluğu.</summary>
    public const int MessageMaxLength = 1000;

    private ProductImportError()
    {
        ProductMainId = string.Empty;
        Message = string.Empty;
    }

    internal ProductImportError(Guid id, string productMainId, string? barcode, string message)
        : base(id)
    {
        ProductMainId = productMainId;
        Barcode = barcode;
        Message = message;
    }

    /// <summary>Gets pazaryerindeki ana ürün (grup) tanımlayıcısı.</summary>
    public string ProductMainId { get; private set; }

    /// <summary>Gets hataya konu barkod; opsiyonel.</summary>
    public string? Barcode { get; private set; }

    /// <summary>Gets hata mesajı.</summary>
    public string Message { get; private set; }
}
