using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Connections;

/// <summary>Tenant'ın bir pazaryerine ait API kimlik bilgilerini yöneten kök aggregate.</summary>
public sealed class MarketplaceConnection : AggregateRoot<Guid>
{
    private MarketplaceConnection()
    {
        Marketplace = null!;
    }

    private MarketplaceConnection(
        Guid id,
        Guid tenantId,
        Marketplace marketplace,
        string? sellerId,
        string apiKey,
        string? apiSecret,
        bool isEnabled)
        : base(id)
    {
        TenantId = tenantId;
        Marketplace = marketplace;
        SellerId = sellerId;
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        IsEnabled = isEnabled;
    }

    /// <summary>Gets tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets bağlı pazaryeri anahtarı.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets satıcı / tedarikçi tanımlayıcısı.</summary>
    public string? SellerId { get; private set; }

    /// <summary>Gets API anahtarı.</summary>
    public string ApiKey { get; private set; } = string.Empty;

    /// <summary>Gets API gizli anahtarı; opsiyonel.</summary>
    public string? ApiSecret { get; private set; }

    /// <summary>Gets a value indicating whether bağlantının etkin olup olmadığı.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Yeni pazaryeri bağlantısı oluşturur.</summary>
    public static Result<MarketplaceConnection> Create(
        Guid tenantId,
        Marketplace marketplace,
        string? sellerId,
        string apiKey,
        string? apiSecret,
        bool isEnabled)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<MarketplaceConnection>(Error.Validation("Tenant id is required."));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Failure<MarketplaceConnection>(Error.Validation("Api key is required."));
        }

        var connection = new MarketplaceConnection(
            Guid.NewGuid(),
            tenantId,
            marketplace,
            NormalizeOptional(sellerId),
            apiKey.Trim(),
            NormalizeOptional(apiSecret),
            isEnabled);

        return Result.Success(connection);
    }

    /// <summary>Bağlantı kimlik bilgilerini günceller.</summary>
    public Result Update(
        string? sellerId,
        string apiKey,
        string? apiSecret,
        bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Failure(Error.Validation("Api key is required."));
        }

        SellerId = NormalizeOptional(sellerId);
        ApiKey = apiKey.Trim();
        ApiSecret = NormalizeOptional(apiSecret);
        IsEnabled = isEnabled;
        return Result.Success();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
