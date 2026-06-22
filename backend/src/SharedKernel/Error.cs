namespace SharedKernel;

/// <summary>
/// Hata kodu, mesajı ve isteğe bağlı doğrulama hatalarını temsil eden kayıt.
/// </summary>
public sealed record Error(string Code, string Message, IReadOnlyList<ValidationError>? ValidationErrors = null)
{
    public static Error Validation(string message, IReadOnlyList<ValidationError>? validationErrors = null) =>
        new(ErrorCodes.Validation, message, validationErrors);

    public static Error NotFound(string message) => new(ErrorCodes.NotFound, message);

    public static Error Conflict(string message) => new(ErrorCodes.Conflict, message);

    public static Error Failure(string message) => new(ErrorCodes.Failure, message);
}
