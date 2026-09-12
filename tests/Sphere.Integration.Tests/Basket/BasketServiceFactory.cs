using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sphere.Integration.Tests.Basket;

public sealed class BasketServiceFactory(PostgresFixture postgresFixture)
    : WebApplicationFactory<BasketServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:basket", postgresFixture.BasketConnectionString);
    }
}
