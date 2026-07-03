using SharedKernel;

namespace Identity.Domain.Tenants;

/// <summary>Kullanıcı ↔ tenant üyelik ilişkisi.</summary>
public sealed class TenantMembership : Entity<Guid>
{
    private TenantMembership()
    {
    }

    private TenantMembership(Guid id, Guid tenantId, Guid userId, bool isPrimary, DateTimeOffset joinedAt)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        IsPrimary = isPrimary;
        JoinedAt = joinedAt;
    }

    /// <summary>Gets tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets kullanıcı kimliği.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets a value indicating whether girişte varsayılan tenant olup olmadığı.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets üyelik başlangıç zamanı.</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>Yeni üyelik oluşturur.</summary>
    public static Result<TenantMembership> Create(
        Guid tenantId,
        Guid userId,
        bool isPrimary,
        DateTimeOffset joinedAt)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<TenantMembership>(Error.Validation("Tenant id is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<TenantMembership>(Error.Validation("User id is required."));
        }

        return Result.Success(new TenantMembership(Guid.NewGuid(), tenantId, userId, isPrimary, joinedAt));
    }
}
