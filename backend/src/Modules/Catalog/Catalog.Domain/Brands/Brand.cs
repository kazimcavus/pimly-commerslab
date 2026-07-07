using SharedKernel;

namespace Catalog.Domain.Brands;

/// <summary>
/// Ürünlerin bağlanabileceği düz (hiyerarşisiz) markayı yöneten kök varlık.
/// </summary>
public sealed class Brand : AggregateRoot<Guid>
{
    private Brand()
    {
    }

    private Brand(Guid id, string name, string? code)
        : base(id)
    {
        Name = name;
        Code = code;
    }

    /// <summary>Gets markanın görünen adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets markanın opsiyonel kodu (ör. pazaryeri marka kimliği).</summary>
    public string? Code { get; private set; }

    /// <summary>Yeni marka oluşturur.</summary>
    /// <param name="name">Marka adı.</param>
    /// <param name="code">Opsiyonel marka kodu.</param>
    public static Result<Brand> Create(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Brand>(Error.Validation("Brand name is required."));
        }

        var brand = new Brand(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim());

        return Result.Success(brand);
    }

    /// <summary>Marka adını ve opsiyonel kodunu günceller.</summary>
    /// <param name="name">Yeni marka adı.</param>
    /// <param name="code">Yeni marka kodu; boş bırakılırsa null olur.</param>
    public Result Rename(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Brand name is required."));
        }

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        return Result.Success();
    }
}
