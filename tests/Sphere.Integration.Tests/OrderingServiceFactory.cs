using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Ordering.Application.Pricing;
using Sphere.Ordering.Infrastructure;

namespace Sphere.Integration.Tests;

internal sealed class OrderingServiceFactory(
    PostgresContainer fixture, HttpClient catalogClient, string? kafkaBootstrap = null)
    : WebApplicationFactory<OrderingServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ordering", fixture.OrderingConnectionString);
        // why: the module refuses to start without them; the catalog value is
        // unused because the client below already carries the test address.
        builder.UseSetting("Catalog:BaseUrl", "http://catalog.test");
        builder.UseSetting("Kafka:BootstrapServers", kafkaBootstrap ?? "unused:9092");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductPriceReader>();
            services.AddSingleton<IProductPriceReader>(
                new CatalogHttpPriceReader(catalogClient));

            if (kafkaBootstrap is null)
            {
                // why: without a broker the loop would spin against nothing —
                // surgical removal of ONE hosted service; the rest stay.
                var consumer = services.Single(d =>
                    d.ImplementationType == typeof(BasketCheckedOutConsumer));
                services.Remove(consumer);
            }
        });
    }
}
