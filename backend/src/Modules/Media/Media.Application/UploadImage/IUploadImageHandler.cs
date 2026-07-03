using Media.Application.Contracts;
using SharedKernel;

namespace Media.Application.UploadImage;

/// <summary>Görsel yükleme işlemini yürüten handler arayüzü.</summary>
public interface IUploadImageHandler
{
    /// <summary>Komutu işler ve yüklenen görsel meta verisini döndürür.</summary>
    Task<Result<UploadImageResultDto>> ExecuteAsync(
        UploadImageCommand command,
        CancellationToken cancellationToken = default);
}
