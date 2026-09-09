using Sphere.Ordering.Application.Checkout;
using System.Net.Http.Json;

namespace Sphere.Ordering.Infrastructure;

public sealed class CatalogHttpPriceReader(HttpClient httpClient) : IProductPriceReader
{
    private sealed record WireLine(Guid ProductId, string Name, decimal Price);

    public async Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/internal/prices",
            new { productIds }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var lines = await response.Content.ReadFromJsonAsync<List<WireLine>>(cancellationToken) ?? [];

        return lines.ToDictionary(
            l => l.ProductId,
            l => new ProductPrice(l.ProductId, l.Name, l.Price));
    }
}
