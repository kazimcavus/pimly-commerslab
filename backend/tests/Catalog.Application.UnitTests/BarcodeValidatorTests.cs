using Catalog.Application.Barcodes.UpdateBarcodeSequence;
using Catalog.Application.Products.CreateProduct;
using FluentAssertions;
using SharedKernel;

namespace Catalog.Application.UnitTests;

/// <summary>UpdateBarcodeSequenceCommandValidator için smoke testleri.</summary>
public class UpdateBarcodeSequenceCommandValidatorTests
{
    private readonly UpdateBarcodeSequenceCommandValidator _validator = new();

    [Fact]
    public void Validate_ZeroNextValue_Fails()
    {
        var result = _validator.Validate(new UpdateBarcodeSequenceCommand(0, false));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NextValue");
    }

    [Fact]
    public void Validate_PositiveNextValue_Succeeds()
    {
        var result = _validator.Validate(new UpdateBarcodeSequenceCommand(8690000001, true));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>CreateProductItemInputValidator barkod kuralları için testler.</summary>
public class CreateProductItemInputValidatorTests
{
    private readonly CreateProductItemInputValidator _validator = new();

    [Fact]
    public void Validate_EmptyBarcode_Fails()
    {
        var result = _validator.Validate(ValidItem() with { Barcode = "  " });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Validate_NumericBarcode_Succeeds()
    {
        var result = _validator.Validate(ValidItem() with { Barcode = "8690000001" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NonNumericBarcode_Fails()
    {
        var result = _validator.Validate(ValidItem() with { Barcode = "BC-001" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    private static CreateProductItemInput ValidItem() =>
        new(null, "8690000001", null, null, null, null, 10m, null, 5, null, null);
}
