using FluentAssertions;
using Media.Application.UploadImage;
using Media.Application.Validation;

namespace Media.Application.UnitTests;

/// <summary>UploadImageCommandValidator için birim testleri.</summary>
public class UploadImageCommandValidatorTests
{
    private readonly UploadImageCommandValidator _validator = new();

    [Fact]
    public void Validate_ProductWithinLimit_Succeeds()
    {
        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = _validator.Validate(new UploadImageCommand(stream, 1024, UploadPurpose.Product));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SwatchOverLimit_Fails()
    {
        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = _validator.Validate(new UploadImageCommand(
            stream,
            (512 * 1024) + 1,
            UploadPurpose.Swatch));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptySize_Fails()
    {
        using var stream = new MemoryStream();
        var result = _validator.Validate(new UploadImageCommand(stream, 0, UploadPurpose.Product));
        result.IsValid.Should().BeFalse();
    }
}
