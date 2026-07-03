namespace SharedKernel;

/// <summary>API ve domain katmanlarında kullanılan üst düzey hata kodları.</summary>
public static class ErrorCodes
{
    /// <summary>İstek veya alan doğrulaması başarısız olduğunda kullanılır.</summary>
    public const string Validation = "validation";

    /// <summary>İstenen kaynak bulunamadığında kullanılır.</summary>
    public const string NotFound = "not_found";

    /// <summary>İş kuralı çakışması veya geçersiz durum geçişinde kullanılır.</summary>
    public const string Conflict = "conflict";

    /// <summary>Genel işlem hatası durumunda kullanılır.</summary>
    public const string Failure = "failure";

    /// <summary>Beklenmeyen sunucu hatası durumunda kullanılır.</summary>
    public const string InternalError = "internal_error";

    /// <summary>Kimlik doğrulama veya yetkilendirme başarısız olduğunda kullanılır.</summary>
    public const string Unauthorized = "unauthorized";
}
