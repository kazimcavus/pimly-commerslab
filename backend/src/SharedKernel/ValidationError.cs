namespace SharedKernel;

/// <summary>
/// Belirli bir alan için doğrulama hatasını temsil eden kayıt.
/// </summary>
/// <param name="Field">Hatalı alanın adı.</param>
/// <param name="Code">Alan düzeyinde hata kodu (<see cref="ValidationErrorCodes"/>).</param>
/// <param name="Message">İnsan tarafından okunabilir hata mesajı.</param>
public sealed record ValidationError(string Field, string Code, string Message);
