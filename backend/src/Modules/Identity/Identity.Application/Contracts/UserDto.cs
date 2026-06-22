namespace Identity.Application.Contracts;

/// <summary>API yanıtlarında dönen kullanıcı özeti.</summary>
public sealed record UserDto(Guid Id, string Email, string Name);
