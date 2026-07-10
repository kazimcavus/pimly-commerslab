using SharedKernel;

namespace Channels.Domain.Publications;

/// <summary>Ürün yayını sırasında tek bir kalem için oluşan hata kaydı.</summary>
public sealed class ProductPublicationError : Entity<Guid>
{
    /// <summary>Hata mesajının izin verilen en büyük uzunluğu.</summary>
    public const int MessageMaxLength = 1000;

    private ProductPublicationError()
    {
        Message = string.Empty;
    }

    internal ProductPublicationError(Guid id, Guid productItemId, string message)
        : base(id)
    {
        ProductItemId = productItemId;
        Message = message;
    }

    /// <summary>Gets hataya konu satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets hata mesajı.</summary>
    public string Message { get; private set; }
}
