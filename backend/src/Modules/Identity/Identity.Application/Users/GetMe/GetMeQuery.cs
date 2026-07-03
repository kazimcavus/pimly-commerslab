namespace Identity.Application.Users.GetMe;

/// <summary>Aktif kullanıcı bilgisini getirme sorgusu.</summary>
public sealed record GetMeQuery(Guid UserId, Guid TenantId);
