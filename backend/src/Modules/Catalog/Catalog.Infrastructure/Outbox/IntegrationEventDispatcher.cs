using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Catalog.Infrastructure.Outbox;

/// <summary>Bir integration olayını, DI'da kayıtlı tüm handler'larına iletir.</summary>
public sealed class IntegrationEventDispatcher(IServiceProvider serviceProvider)
{
    /// <summary>Olayı ilgili tüm <see cref="IIntegrationEventHandler{TEvent}"/> handler'larına dağıtır.</summary>
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
