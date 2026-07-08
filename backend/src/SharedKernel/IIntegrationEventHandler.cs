namespace SharedKernel;

/// <summary>
/// Bir integration olayını tüketen handler. Uydu context'ler (Pricing, Inventory) kendi
/// tepkilerini bu arabirimi uygulayarak kaydeder; dispatcher olayı ilgili tüm handler'lara iletir.
/// </summary>
/// <typeparam name="TEvent">Tüketilen integration olayı tipi.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    /// <summary>Olayı işler.</summary>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
