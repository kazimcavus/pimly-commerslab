namespace Media.Application.Contracts;

/// <summary>Görsel yükleme API yanıt modeli.</summary>
public sealed record UploadImageResultDto(string Url, string ContentType, long SizeBytes);
