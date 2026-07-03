using Catalog.Application.Options;
using Catalog.Application.Products.AddProductImage;
using FluentAssertions;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Catalog.Application.UnitTests;

/// <summary>AddProductImageCommandValidator için birim testleri.</summary>
public class AddProductImageCommandValidatorTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly AddProductImageCommandValidator _validator = new(
        Microsoft.Extensions.Options.Options.Create(new MediaUrlOptions { AllowedUrlPrefix = "/media/" }),
        new TestTenantContext(TenantId));

    [Fact]
    public void Validate_AllowedUrl_Succeeds()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ExternalUrl_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Url = "https://example.com/photo.jpg" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url" && e.ErrorCode == ValidationErrorCodes.InvalidFormat);
    }

    [Fact]
    public void Validate_OtherTenantMediaUrl_Fails()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            Url = $"/media/{Guid.NewGuid():N}/ab/cd/sample.jpg",
        });
        result.IsValid.Should().BeFalse();
    }

    private static AddProductImageCommand ValidCommand() =>
        new(
            Guid.NewGuid(),
            $"/media/{TenantId:N}/ab/cd/sample.jpg",
            0,
            null,
            true,
            null);

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
