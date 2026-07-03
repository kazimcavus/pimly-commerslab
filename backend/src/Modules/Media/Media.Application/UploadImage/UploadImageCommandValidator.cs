using FluentValidation;
using Media.Application.Validation;

namespace Media.Application.UploadImage;

/// <summary>UploadImageCommand için FluentValidation kuralları.</summary>
public sealed class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
{
    public UploadImageCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotNull()
            .WithErrorCode(ValidationErrorCodes.InvalidFormat)
            .WithMessage(ValidationMessages.InvalidFormat("Content"));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithErrorCode(ValidationErrorCodes.OutOfRange)
            .WithMessage(ValidationMessages.InvalidFormat("SizeBytes"));

        RuleFor(x => x)
            .Must(command => command.SizeBytes <= GetMaxBytes(command.Purpose))
            .WithErrorCode(ValidationErrorCodes.OutOfRange)
            .WithMessage(command => ValidationMessages.MaxSize("File", GetMaxBytes(command.Purpose)));
    }

    private static long GetMaxBytes(UploadPurpose purpose) =>
        purpose == UploadPurpose.Swatch
            ? MediaValidationRules.SwatchMaxBytes
            : MediaValidationRules.ProductMaxBytes;
}
