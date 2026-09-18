using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Basket.Contracts;

namespace Sphere.Integration.Tests.Basket;

public sealed class BasketServiceFactory(PostgresFixture postgresFixture, string? kafkaBootstrap = null)
    : WebApplicationFactory<BasketServiceMarker>
{
    public RecordingBasketEvents Events { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:basket", postgresFixture.BasketConnectionString);

        builder.UseSetting("Kafka:BootstrapServers", kafkaBootstrap ?? "unused:9092");
        builder.UseSetting("Kafka:Source", "/test/basket");

        if (kafkaBootstrap is null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBasketEvents>();
                services.AddSingleton<IBasketEvents>(Events);
            });
        }
    }
}

public sealed class RecordingBasketEvents : IBasketEvents
{
    public List<(BasketSnapshot Snapshot, Guid OrderId)> Published { get; } = [];

    public Task EnsureTopicAsync() => Task.CompletedTask;

    public Task PublishCheckedOutAsync(BasketSnapshot basket, Guid checkoutId, CancellationToken cancellationToken)
    {
        Published.Add((basket, checkoutId));
        return Task.CompletedTask;
    }
}
