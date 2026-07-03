namespace Media.Application.UploadImage;

/// <summary>Görsel yükleme komutu.</summary>
public sealed record UploadImageCommand(Stream Content, long SizeBytes, UploadPurpose Purpose);
