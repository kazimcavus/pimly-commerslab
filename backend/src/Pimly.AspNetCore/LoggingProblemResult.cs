using Microsoft.AspNetCore.Http;
using SharedKernel;

namespace Pimly.AspNetCore;

/// <summary>
/// ProblemDetails yanıtı yazar; hatayı yapılandırılmış loglar ve trace kimliğini yanıta ekler.
/// Tüm modül endpoint'lerindeki <see cref="ResultExtensions"/> hata dönüşleri bu türü kullanır.
/// </summary>
internal sealed class LoggingProblemResult(Error error) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Items[HttpContextObservability.FailureLoggedItemKey] = true;

        var statusCode = ProblemDetailsFactory.MapStatusCode(error.Code);
        var traceId = HttpContextObservability.GetTraceId(httpContext);

        ApiFailureLogger.Log(httpContext, error, statusCode);

        var problem = ProblemDetailsFactory.Create(error, traceId);

        HttpContextObservability.AppendTraceIdHeader(httpContext, traceId);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, httpContext.RequestAborted);
    }
}
