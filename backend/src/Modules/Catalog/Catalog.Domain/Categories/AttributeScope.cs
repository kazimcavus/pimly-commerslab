namespace Catalog.Domain.Categories;

/// <summary>
/// Kategoriye atanan özniteliğin ürün yapısındaki seviyesi. Pazaryeri import'unda kaynak
/// bayraklarından (varianter/slicer) ve değer dağılımından türetilir; kullanıcı sonradan düzenleyebilir.
/// </summary>
public enum AttributeScope
{
    /// <summary>Değer model (ürün) başına bir kez seçilir.</summary>
    Model = 0,

    /// <summary>Değer slicer (ör. renk) değeri başına seçilir; bölünen her ürün kendi değerini taşır.</summary>
    Slicer = 1,

    /// <summary>Değer satılabilir kalem (varyant) başına seçilir.</summary>
    Item = 2,
}
