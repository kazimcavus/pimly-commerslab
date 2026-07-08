using Catalog.Domain.Products.Events;
using Catalog.Infrastructure.Outbox;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Catalog.IntegrationTests;

/// <summary>Integration olay dağıtımının çekirdek yönlendirme mantığı için birim testleri (DB gerekmez).</summary>
public class IntegrationEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_InvokesRegisteredHandler()
    {
        var handler = new RecordingHandler();
        await using var provider = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<ProductItemCreated>>(handler)
            .AddScoped<IntegrationEventDispatcher>()
            .BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IntegrationEventDispatcher>();
        var integrationEvent = new ProductItemCreated(Guid.NewGuid(), Guid.NewGuid());

        await dispatcher.DispatchAsync(integrationEvent);

        handler.Received.Should().ContainSingle().Which.Should().Be(integrationEvent);
    }

    [Fact]
    public void TypeRegistry_ResolvesKnownType_AndReturnsNullForUnknown()
    {
        var registry = new IntegrationEventTypeRegistry([typeof(ProductItemCreated)]);

        registry.Resolve(typeof(ProductItemCreated).FullName!).Should().Be<ProductItemCreated>();
        registry.Resolve("Nonexistent.Type").Should().BeNull();
    }

    private sealed class RecordingHandler : IIntegrationEventHandler<ProductItemCreated>
    {
        public List<ProductItemCreated> Received { get; } = [];

        public Task HandleAsync(ProductItemCreated integrationEvent, CancellationToken cancellationToken = default)
        {
            Received.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
