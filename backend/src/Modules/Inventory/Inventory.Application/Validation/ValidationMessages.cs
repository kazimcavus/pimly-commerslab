namespace Inventory.Application.Validation;

/// <summary>Doğrulama hata mesajı şablonları.</summary>
internal static class ValidationMessages
{
    public static string InvalidId(string field) => $"{field} must be a valid identifier.";
}
