namespace SharedKernel.Tenancy;

/// <summary>HTTP isteği bağlamındaki aktif tenant kimliği.</summary>
public interface ITenantContext
{
    /// <summary>Gets JWT'den çözümlenen tenant kimliği.</summary>
    Guid TenantId { get; }
}
