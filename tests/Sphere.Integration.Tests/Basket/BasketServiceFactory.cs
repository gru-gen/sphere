using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Basket.Contracts;

namespace Sphere.Integration.Tests.Basket;

public sealed class BasketServiceFactory(PostgresFixture postgresFixture, string? kafkaBootstrap = null)
    : WebApplicationFactory<BasketServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:basket", postgresFixture.BasketConnectionString);

        builder.UseSetting("Kafka:BootstrapServers", kafkaBootstrap ?? "unused:9092");

        if (kafkaBootstrap is null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBasketEvents>();
                services.AddSingleton<IBasketEvents>(new NoBasketEvents());
            });
        }
    }

    // summary: publishing is not the subject of most tests — this stand-in
    // keeps them fast and honest instead of timing out against nothing.
    private sealed class NoBasketEvents : IBasketEvents
    {
        public Task EnsureTopicAsync() => Task.CompletedTask;

        public Task PublishCheckedOutAsync(BasketSnapshot snapshot, CancellationToken ct)
            => Task.CompletedTask;
    }
}
