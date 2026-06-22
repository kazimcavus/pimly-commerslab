namespace Catalog.Application.Validation;

/// <summary>Doğrulama hata mesajı şablonları.</summary>
internal static class ValidationMessages
{
    public static string Required(string field) => $"{field} is required.";

    public static string MaxLength(string field, int maxLength) =>
        $"{field} must not exceed {maxLength} characters.";

    public static string InvalidEnum(string field) => $"{field} has an invalid value.";

    public static string InvalidId(string field) => $"{field} must be a valid identifier.";
}
