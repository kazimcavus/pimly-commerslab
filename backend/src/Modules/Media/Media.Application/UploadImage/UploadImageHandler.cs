using FluentValidation;
using Media.Application.Contracts;
using Media.Application.Options;
using Media.Application.Storage;
using Media.Application.Validation;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Media.Application.UploadImage;

/// <summary>Görsel yükleme işlemini yürüten handler.</summary>
public sealed class UploadImageHandler(
    IValidator<UploadImageCommand> validator,
    IBlobStorage blobStorage,
    IImageContentTypeDetector contentTypeDetector,
    IOptions<MediaOptions> options,
    ITenantContext tenantContext) : IUploadImageHandler
{
    /// <inheritdoc/>
    public async Task<Result<UploadImageResultDto>> ExecuteAsync(
        UploadImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<UploadImageResultDto>(validationResult.Error);
        }

        var contentType = contentTypeDetector.Detect(command.Content);
        if (contentType is null)
        {
            return Result.Failure<UploadImageResultDto>(
                Error.Validation("Unsupported or invalid image format."));
        }

        if (command.Content.CanSeek)
        {
            command.Content.Position = 0;
        }

        var stored = await blobStorage.SaveAsync(
            command.Content,
            contentType,
            tenantContext.TenantId,
            cancellationToken);

        var url = BuildPublicUrl(stored.StorageKey, options.Value);

        return Result.Success(new UploadImageResultDto(url, stored.ContentType, stored.SizeBytes));
    }

    private static string BuildPublicUrl(string storageKey, MediaOptions mediaOptions)
    {
        var path = $"/media/{storageKey.Replace('\\', '/')}";

        if (string.IsNullOrWhiteSpace(mediaOptions.PublicBaseUrl))
        {
            return path;
        }

        return $"{mediaOptions.PublicBaseUrl.TrimEnd('/')}{path}";
    }
}
