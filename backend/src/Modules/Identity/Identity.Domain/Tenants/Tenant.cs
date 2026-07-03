using SharedKernel;

namespace Identity.Domain.Tenants;

/// <summary>Pimly SaaS müşteri organizasyonu.</summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    public static readonly Guid DevAcmeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Tenant()
    {
    }

    private Tenant(Guid id, string name, DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        CreatedAt = createdAt;
    }

    /// <summary>Gets organizasyon adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets oluşturulma zamanı.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Yeni tenant oluşturur.</summary>
    public static Result<Tenant> Create(string name, DateTimeOffset createdAt) =>
        Create(Guid.NewGuid(), name, createdAt);

    /// <summary>Belirtilen kimlik ile yeni tenant oluşturur.</summary>
    public static Result<Tenant> Create(Guid id, string name, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<Tenant>(Error.Validation("Tenant id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tenant>(Error.Validation("Tenant name is required."));
        }

        if (name.Trim().Length > 200)
        {
            return Result.Failure<Tenant>(Error.Validation("Tenant name cannot exceed 200 characters."));
        }

        return Result.Success(new Tenant(id, name.Trim(), createdAt));
    }

    /// <summary>Geliştirme ortamı için sabit Acme tenant kaydını oluşturur.</summary>
    public static Tenant CreateDevAcme(DateTimeOffset createdAt) =>
        Create(DevAcmeId, "Acme", createdAt).Value;
}
