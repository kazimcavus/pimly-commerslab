namespace Channels.Application.Readiness.GetProductReadiness;

/// <summary>Bir ürünün bağlı pazaryerlerine yayın hazırlığını sorgular.</summary>
public sealed record GetProductReadinessQuery(Guid ProductId);
