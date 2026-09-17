using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Sphere.Notification.Service.Infrastructure;

namespace Sphere.Integration.Tests;

internal sealed class NotificationServiceFactory(
    PostgresFixture fixture, string rabbitHost, ushort rabbitPort)
    : WebApplicationFactory<NotificationServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:notification", fixture.NotificationConnectionString);
        builder.UseSetting("Rabbit:Host", rabbitHost);
        builder.UseSetting("Rabbit:Port", rabbitPort.ToString());
        builder.UseSetting("Rabbit:User", "sphere");
        builder.UseSetting("Rabbit:Pass", "sphere-dev");
        builder.UseSetting("Kafka:BootstrapServers", "unused:9092");

        builder.ConfigureServices(services =>
        {
            var logger = services.Single(d =>
                d.ImplementationType == typeof(BasketCheckedOutLogger));
            services.Remove(logger);
        });
    }
}
