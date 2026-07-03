namespace Media.Application.Storage;

/// <summary>Kaydedilmiş blob meta verisi.</summary>
public sealed record StoredBlob(string StorageKey, string ContentType, long SizeBytes);
