using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Ordering.Application.Checkout;
using Sphere.Ordering.Infrastructure;

namespace Sphere.Integration.Tests;

internal sealed class MonolithFactory(
    PostgresFixture postgresFixture, HttpClient catalogClient, HttpClient basketClient)
    : WebApplicationFactory<HostMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ordering", postgresFixture.ConnectionString);

        builder.UseSetting("Catalog:BaseUrl", "http://catalog.test");
        builder.UseSetting("Basket:BaseUrl", "http://basket.test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductPriceReader>();
            services.AddSingleton<IProductPriceReader>(
                new CatalogHttpPriceReader(catalogClient));

            services.RemoveAll<ICustomerBasket>();
            services.AddSingleton<ICustomerBasket>(
                new HttpCustomerBasket(basketClient));
        });
    }
}
