namespace Identity.Infrastructure.Auth;

/// <summary>JWT yapılandırma seçenekleri.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    public string Secret { get; set; } = string.Empty;

    public int ExpirationHours { get; set; } = 24;
}
