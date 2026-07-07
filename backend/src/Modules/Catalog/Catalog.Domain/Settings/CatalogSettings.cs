using SharedKernel;

namespace Catalog.Domain.Settings;

/// <summary>Katalog davranış tercihleri — tenant başına tek satır.</summary>
public sealed class CatalogSettings : Entity<int>
{
    public const int SingletonId = 1;

    /// <summary>Ayraç değeri ürün adının sonuna eklenir: "Abiye Elbise - Beyaz".</summary>
    public const string NamePositionSuffix = "suffix";

    /// <summary>Ayraç değeri ürün adının başına eklenir: "Beyaz Abiye Elbise".</summary>
    public const string NamePositionPrefix = "prefix";

    private CatalogSettings()
    {
    }

    private CatalogSettings(int id, string slicerNamePosition)
        : base(id)
    {
        SlicerNamePosition = slicerNamePosition;
    }

    /// <summary>Gets ayraçlı (renk vb.) ürünlerde değer adının konumu.</summary>
    public string SlicerNamePosition { get; private set; } = NamePositionSuffix;

    /// <summary>Varsayılan tercihlerle başlangıç ayarlarını oluşturur.</summary>
    public static CatalogSettings CreateInitial() =>
        new(SingletonId, NamePositionSuffix);

    /// <summary>Tercihleri günceller.</summary>
    /// <param name="slicerNamePosition">Ayraç değeri ad konumu; "suffix" veya "prefix".</param>
    public Result Update(string slicerNamePosition)
    {
        if (slicerNamePosition is not (NamePositionSuffix or NamePositionPrefix))
        {
            return Result.Failure(Error.Validation("Slicer name position must be 'suffix' or 'prefix'."));
        }

        SlicerNamePosition = slicerNamePosition;
        return Result.Success();
    }
}
