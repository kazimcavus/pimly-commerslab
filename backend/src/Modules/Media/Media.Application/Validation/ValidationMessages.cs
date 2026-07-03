namespace Media.Application.Validation;

/// <summary>Media modülü doğrulama mesajları.</summary>
internal static class ValidationMessages
{
    public static string MaxSize(string fieldName, long maxBytes) =>
        $"{fieldName} must not exceed {maxBytes} bytes.";

    public static string InvalidFormat(string fieldName) =>
        $"{fieldName} has an invalid format.";
}
