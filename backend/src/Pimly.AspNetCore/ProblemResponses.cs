using Microsoft.AspNetCore.Http;
using SharedKernel;

namespace Pimly.AspNetCore;

/// <summary>Handler dışında doğrudan ProblemDetails dönen endpoint'ler için ortak yanıtlar.</summary>
public static class ProblemResponses
{
    /// <summary>Domain <see cref="Error"/> kaydından loglanan ProblemDetails yanıtı üretir.</summary>
    public static IResult FromError(Error error) => new LoggingProblemResult(error);

    /// <summary>Doğrulama hatası yanıtı üretir ve yapılandırılmış log kaydı oluşturur.</summary>
    public static IResult Validation(
        string message,
        IReadOnlyList<ValidationError>? validationErrors = null) =>
        new LoggingProblemResult(Error.Validation(message, validationErrors));
}
