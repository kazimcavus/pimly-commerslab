using Media.Application.UploadImage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;

namespace Media.Api;

/// <summary>Media modülü REST API uç noktalarını kaydeder.</summary>
public static class MediaEndpoints
{
    /// <summary>Media modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    public static RouteGroupBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/media")
            .WithTags("Media")
            .RequireAuthorization();

        group.MapPost("/uploads", async (
            IFormFile file,
            [FromQuery(Name = "purpose")] string? purpose,
            IUploadImageHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (file is null || file.Length == 0)
            {
                return ProblemResponses.Validation("File is required.");
            }

            var uploadPurpose = ParsePurpose(purpose);
            await using var stream = file.OpenReadStream();
            var result = await handler.ExecuteAsync(
                new UploadImageCommand(stream, file.Length, uploadPurpose),
                cancellationToken);

            return result.ToHttpResult();
        })
        .DisableAntiforgery();

        return group;
    }

    private static UploadPurpose ParsePurpose(string? purpose) =>
        purpose?.Trim().ToLowerInvariant() switch
        {
            "swatch" => UploadPurpose.Swatch,
            _ => UploadPurpose.Product,
        };
}
