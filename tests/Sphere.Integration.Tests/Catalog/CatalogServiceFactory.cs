using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sphere.Integration.Tests.Catalog;

public sealed class CatalogServiceFactory(PostgresFixture postgresFixture)
    : WebApplicationFactory<CatalogServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:catalog", postgresFixture.CatalogConnectionString);
    }
}
