using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Ordering.Application.Pricing;
using Sphere.Ordering.Infrastructure;

namespace Sphere.Integration.Tests.Ordering;

internal sealed class OrderingServiceFactory(
    PostgresFixture postgresFixture, HttpClient catalogClient, HttpClient basketClient, string? kafkaBootstrap = null)
    : WebApplicationFactory<OrderingServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ordering", postgresFixture.OrderingConnectionString);

        builder.UseSetting("Catalog:BaseUrl", "http://catalog.test");
        builder.UseSetting("Kafka:BootstrapServers", kafkaBootstrap ?? "unused:9092");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductPriceReader>();
            services.AddSingleton<IProductPriceReader>(
                new CatalogHttpPriceReader(catalogClient));

            if (kafkaBootstrap is null)
            {
                var consumer = services.Single(d =>
                    d.ImplementationType == typeof(BasketCheckedOutConsumer));
                services.Remove(consumer);
            }
        });
    }
}
