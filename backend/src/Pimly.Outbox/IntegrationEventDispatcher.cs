using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Pimly.Outbox;

/// <summary>Bir integration olayını, DI'da kayıtlı tüm handler'larına iletir.</summary>
public sealed class IntegrationEventDispatcher(IServiceProvider serviceProvider)
{
    /// <summary>Olayı ilgili tüm <see cref="IIntegrationEventHandler{TEvent}"/> handler'larına dağıtır.</summary>
    /// <param name="integrationEvent">Dağıtılacak olay.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Dağıtım görevi.</returns>
    public async Task DispatchAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());
        var handleMethod = handlerType.GetMethod("HandleAsync")!;

        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            if (handler is null)
            {
                continue;
            }

            await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;
        }
    }
}
