using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Media upload ve ürün galerisi API uç noktaları için entegrasyon testleri.</summary>
public class MediaApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task UploadImage_ThenServeStaticFile_HappyPath()
    {
        var uploadResponse = await UploadPngAsync("product");
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadImageResponse>(CatalogJson.Options);
        uploaded.Should().NotBeNull();
        uploaded!.Url.Should().StartWith("/media/");
        uploaded.Url.Split('/').Should().HaveCountGreaterThan(4, "URL should include tenant segment");
        uploaded.ContentType.Should().Be("image/png");
        uploaded.SizeBytes.Should().BeGreaterThan(0);

        var fileResponse = await Client.GetAsync(uploaded.Url);
        fileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [SkippableFact]
    public async Task UploadImage_InvalidContent_ReturnsValidationError()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("not-an-image"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "bad.txt");

        var response = await Client.PostAsync("/api/v1/media/uploads?purpose=product", content);
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task ProductImageCrud_WithUploadedUrl_HappyPath()
    {
        var uploadResponse = await UploadPngAsync("product");
        var uploaded = (await uploadResponse.Content.ReadFromJsonAsync<UploadImageResponse>(CatalogJson.Options))!;

        var product = await CreateSimpleProductAsync();

        var addImageResponse = await Client.PostAsJsonAsync($"/api/v1/catalog/products/{product.Id}/images", new
        {
            url = uploaded.Url,
            sort_order = 0,
            alt_text = "Front",
            is_primary = true,
            variant_value_id = (Guid?)null,
        });
        addImageResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var image = await addImageResponse.Content.ReadFromJsonAsync<ProductImageResponse>(CatalogJson.Options);
        image!.Url.Should().Be(uploaded.Url);
        image.IsPrimary.Should().BeTrue();

        var getProduct = await Client.GetAsync($"/api/v1/catalog/products/{product.Id}");
        var fetched = await getProduct.Content.ReadFromJsonAsync<ProductWithImagesResponse>(CatalogJson.Options);
        fetched!.Images.Should().ContainSingle(i => i.Id == image.Id);

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/product-images/{image.Id}", new
        {
            url = uploaded.Url,
            sort_order = 1,
            alt_text = "Updated",
            is_primary = true,
            variant_value_id = (Guid?)null,
        });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/catalog/product-images/{image.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
    }

    [SkippableFact]
    public async Task AddProductImage_ExternalUrl_ReturnsValidationError()
    {
        var product = await CreateSimpleProductAsync();

        var response = await Client.PostAsJsonAsync($"/api/v1/catalog/products/{product.Id}/images", new
        {
            url = "https://example.com/photo.jpg",
            sort_order = 0,
            is_primary = false,
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
    }

    [SkippableFact]
    public async Task VariantValueImageUrl_WithUploadedSwatch_Persists()
    {
        var uploadResponse = await UploadPngAsync("swatch");
        var uploaded = (await uploadResponse.Content.ReadFromJsonAsync<UploadImageResponse>(CatalogJson.Options))!;

        var variant = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Swatch-{Guid.NewGuid():N}",
            selection_style = "color",
            sort_order = 0,
            slicer = false,
        });
        var variantType = (await variant.Content.ReadFromJsonAsync<VariantTypeResponse>(CatalogJson.Options))!;

        var valueResponse = await Client.PostAsJsonAsync($"/api/v1/catalog/variants/{variantType.Id}/values", new
        {
            label = "Navy",
            color = "#001133",
            image_url = uploaded.Url,
            sort_order = 0,
        });
        valueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var value = await valueResponse.Content.ReadFromJsonAsync<VariantValueWithImageResponse>(CatalogJson.Options);
        value!.ImageUrl.Should().Be(uploaded.Url);

        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{value.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{variantType.Id}");
    }

    private async Task<HttpResponseMessage> UploadPngAsync(string purpose)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(MinimalPngBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");
        return await Client.PostAsync($"/api/v1/media/uploads?purpose={purpose}", content);
    }

    private async Task<ProductWithImagesResponse> CreateSimpleProductAsync()
    {
        var categoryId = await CreateCategoryAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"IMG-{Guid.NewGuid():N}",
            name = "Image Test Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[]
            {
                new
                {
                    barcode = NextNumericBarcode(),
                    price = 9.99m,
                    stock = 1,
                },
            },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await createResponse.Content.ReadFromJsonAsync<ProductWithImagesResponse>(CatalogJson.Options))!;
    }

    private static string NextNumericBarcode() =>
        (9200000000L + Random.Shared.Next(1, 1000000)).ToString(CultureInfo.InvariantCulture);

    private static byte[] MinimalPngBytes() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82,
    ];
}

/// <summary>Medya yükleme API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record UploadImageResponse(string Url, string ContentType, long SizeBytes);

/// <summary>Ürün görseli API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record ProductImageResponse(
    Guid Id,
    string Url,
    int SortOrder,
    string? AltText,
    bool IsPrimary,
    Guid? VariantValueId);

/// <summary>Görselleri ve kalemleriyle birlikte ürün API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record ProductWithImagesResponse(
    Guid Id,
    IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<ItemResponse> Items);

/// <summary>Variant type API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record VariantTypeResponse(Guid Id);

/// <summary>Variant value görsel API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record VariantValueWithImageResponse(Guid Id, string? ImageUrl);
