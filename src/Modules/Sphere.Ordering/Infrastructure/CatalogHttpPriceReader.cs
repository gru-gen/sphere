using Sphere.Ordering.Application.Checkout;
using System.Net.Http.Json;

namespace Sphere.Ordering.Infrastructure;

// summary: the anti-corruption layer — Catalog's wire format stops HERE.
// The checkout handler sees Ordering's own ProductPrice and nothing else.
public sealed class CatalogHttpPriceReader(HttpClient httpClient) : IProductPriceReader
{
    // why: a private copy of the remote shape. If Catalog renames a field one
    // day, the change is absorbed in this file — not in the domain.
    private sealed record WireLine(Guid ProductId, string Name, decimal Price);

    public async Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/internal/prices",
            new { productIds }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var lines = await response.Content.ReadFromJsonAsync<List<WireLine>>(cancellationToken) ?? [];

        // why: translate at the boundary — wire record in, Ordering record out.
        return lines.ToDictionary(
            l => l.ProductId,
            l => new ProductPrice(l.ProductId, l.Name, l.Price));
    }
}
