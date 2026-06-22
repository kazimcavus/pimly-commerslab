namespace SharedKernel;

/// <summary>API ve domain katmanlarında kullanılan üst düzey hata kodları.</summary>
public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string Failure = "failure";
    public const string InternalError = "internal_error";
    public const string Unauthorized = "unauthorized";
}
