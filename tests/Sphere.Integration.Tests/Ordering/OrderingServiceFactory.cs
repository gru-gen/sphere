using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Ordering.Application.Events;
using Sphere.Ordering.Application.Notifications;
using Sphere.Ordering.Application.Pricing;
using Sphere.Ordering.Infrastructure;

namespace Sphere.Integration.Tests.Ordering;

internal sealed class OrderingServiceFactory(
    PostgresFixture fixture, HttpClient catalogClient,
    string? kafkaBootstrap = null, string? rabbitHost = null, ushort rabbitPort = 0)
    : WebApplicationFactory<OrderingServiceMarker>
{
    // why: broker-free tests still assert "one order, one confirmation ask" —
    // the recorder keeps the commands in a list instead of on a wire.
    public RecordingOrderNotifier Notifier { get; } = new();

    public RecordingOrderingEvents OrderingEvents { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ordering", fixture.OrderingConnectionString);
        // why: the module refuses to start without them; the catalog value is
        // unused because the client below already carries the test address.
        builder.UseSetting("Catalog:BaseUrl", "http://catalog.test");
        builder.UseSetting("Kafka:BootstrapServers", kafkaBootstrap ?? "unused:9092");
        builder.UseSetting("Kafka:Source", "/test/ordering");
        builder.UseSetting("Rabbit:Host", rabbitHost ?? "unused");
        builder.UseSetting("Rabbit:Port", (rabbitPort == 0 ? (ushort)5672 : rabbitPort).ToString());
        builder.UseSetting("Rabbit:User", "shopsphere");
        builder.UseSetting("Rabbit:Pass", "shopsphere-dev");

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

                services.RemoveAll<IOrderingEvents>();
                services.AddSingleton<IOrderingEvents>(OrderingEvents);
            }

            if (rabbitHost is null)
            {
                // why: same surgery for the command door — the recorder takes
                // the port, and the bus hosted services (which would dial a
                // Rabbit that is not there) leave.
                services.RemoveAll<IOrderNotifier>();
                services.AddSingleton<IOrderNotifier>(Notifier);
                var bus = services.Where(d =>
                    d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                    && d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true).ToList();
                foreach (var descriptor in bus)
                {
                    services.Remove(descriptor);
                }
            }
        });
    }
}
