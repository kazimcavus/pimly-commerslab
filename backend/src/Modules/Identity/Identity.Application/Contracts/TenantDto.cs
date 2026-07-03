namespace Identity.Application.Contracts;

/// <summary>API yanıtlarında dönen tenant özeti.</summary>
public sealed record TenantDto(Guid Id, string Name);
