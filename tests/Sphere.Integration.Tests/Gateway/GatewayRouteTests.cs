using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Sphere.Integration.Tests.Gateway;

public sealed class GatewayRouteTests
{
    [Fact]
    public void Every_path_has_a_service_and_nothing_falls_through()
    {
        using var factory = new WebApplicationFactory<GatewayMarker>();
        var config = factory.Services
            .GetRequiredService<IProxyConfigProvider>().GetConfig();

        var catalog = config.Routes.Where(r => r.ClusterId == "catalog").ToList();
        Assert.Equal(2, catalog.Count);
        Assert.Contains(catalog, r => r.Match.Path == "/api/products/{**rest}");
        Assert.Contains(catalog, r => r.Match.Path == "/api/categories/{**rest}");

        var basket = config.Routes.Where(r => r.ClusterId == "basket").ToList();
        Assert.Equal(2, basket.Count);
        Assert.Contains(basket, r => r.Match.Path == "/api/basket/{**rest}");
        // why: checkout is BASKET's door now — the route table records the flip.
        Assert.Contains(basket, r => r.Match.Path == "/api/checkout");

        var ordering = Assert.Single(config.Routes, r => r.ClusterId == "ordering");
        Assert.Equal("/api/orders/{**rest}", ordering.Match.Path);

        // why: the catch-all is GONE — an unknown path now gets the gateway's
        // own 404, because there is no monolith left to hide behind.
        Assert.DoesNotContain(config.Routes, r => r.ClusterId == "monolith");
        Assert.DoesNotContain(config.Routes, r => r.Match.Path!.Contains("catch-all"));

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
        Assert.Equal("http://localhost:5140/",
            clusters["ordering"].Destinations!.Values.Single().Address);
        Assert.False(clusters.ContainsKey("monolith"));
    }
}
