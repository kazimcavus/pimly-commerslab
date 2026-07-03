namespace Media.Application.Validation;

/// <summary>Media modülü için ortak doğrulama sabitleri.</summary>
internal static class MediaValidationRules
{
    public const long SwatchMaxBytes = 512 * 1024;
    public const long ProductMaxBytes = 5 * 1024 * 1024;
}
