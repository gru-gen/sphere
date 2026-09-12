using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Sphere.Integration.Tests.Gateway;

public sealed class GatewayRouteTests
{
    [Fact]
    public void Extracted_paths_go_to_their_services_everything_else_to_the_monolith()
    {
        using var factory = new WebApplicationFactory<GatewayMarker>();
        var config = factory.Services
            .GetRequiredService<IProxyConfigProvider>().GetConfig();

        var catalog = config.Routes.Where(r => r.ClusterId == "catalog").ToList();
        Assert.Equal(2, catalog.Count);
        Assert.Contains(catalog, r => r.Match.Path == "/api/products/{**rest}");
        Assert.Contains(catalog, r => r.Match.Path == "/api/categories/{**rest}");

        var basket = Assert.Single(config.Routes, r => r.ClusterId == "basket");
        Assert.Equal("/api/basket/{**rest}", basket.Match.Path);

        var fallback = Assert.Single(config.Routes, r => r.ClusterId == "monolith");
        Assert.Equal("/{**catch-all}", fallback.Match.Path);
        Assert.True(fallback.Order > catalog[0].Order);
        Assert.True(fallback.Order > basket.Order);

        // why: negative space is part of the contract — no route says internal.
        Assert.DoesNotContain(config.Routes,
            r => r.Match.Path!.Contains("internal"));

        // why: the cache policy is route CONFIG — so it is asserted like config.
        var products = Assert.Single(catalog,
            r => r.Match.Path == "/api/products/{**rest}");
        Assert.Equal("catalog-10s", products.OutputCachePolicy);

        var clusters = config.Clusters.ToDictionary(c => c.ClusterId);
        Assert.Equal("http://localhost:5120/",
            clusters["catalog"].Destinations!.Values.Single().Address);
        Assert.Equal("http://localhost:5130/",
            clusters["basket"].Destinations!.Values.Single().Address);
        Assert.Equal("http://localhost:5110/",
            clusters["monolith"].Destinations!.Values.Single().Address);
    }
}
