namespace SharedKernel;

/// <summary>
/// Hata kodu, mesajı ve isteğe bağlı doğrulama hatalarını temsil eden kayıt.
/// </summary>
/// <param name="Code">Hata kodu (<see cref="ErrorCodes"/>).</param>
/// <param name="Message">İnsan tarafından okunabilir hata mesajı.</param>
/// <param name="ValidationErrors">Alan düzeyinde doğrulama hataları; yalnızca doğrulama hatalarında dolu.</param>
public sealed record Error(string Code, string Message, IReadOnlyList<ValidationError>? ValidationErrors = null)
{
    /// <summary>Doğrulama hatası oluşturur.</summary>
    /// <param name="message">Özet hata mesajı.</param>
    /// <param name="validationErrors">Alan düzeyinde doğrulama hataları.</param>
    /// <returns><see cref="ErrorCodes.Validation"/> kodlu hata.</returns>
    public static Error Validation(string message, IReadOnlyList<ValidationError>? validationErrors = null) =>
        new(ErrorCodes.Validation, message, validationErrors);

    /// <summary>Kaynak bulunamadı hatası oluşturur.</summary>
    /// <param name="message">Özet hata mesajı.</param>
    /// <returns><see cref="ErrorCodes.NotFound"/> kodlu hata.</returns>
    public static Error NotFound(string message) => new(ErrorCodes.NotFound, message);

    /// <summary>Çakışma hatası oluşturur.</summary>
    /// <param name="message">Özet hata mesajı.</param>
    /// <returns><see cref="ErrorCodes.Conflict"/> kodlu hata.</returns>
    public static Error Conflict(string message) => new(ErrorCodes.Conflict, message);

    /// <summary>Yetkilendirme hatası oluşturur.</summary>
    /// <param name="message">Özet hata mesajı.</param>
    /// <returns><see cref="ErrorCodes.Unauthorized"/> kodlu hata.</returns>
    public static Error Unauthorized(string message) => new(ErrorCodes.Unauthorized, message);

    /// <summary>Genel işlem hatası oluşturur.</summary>
    /// <param name="message">Özet hata mesajı.</param>
    /// <returns><see cref="ErrorCodes.Failure"/> kodlu hata.</returns>
    public static Error Failure(string message) => new(ErrorCodes.Failure, message);
}
