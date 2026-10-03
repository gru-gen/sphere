using Sphere.Ordering.Application.Checkout;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace Sphere.Ordering.Infrastructure;

// summary: the anti-corruption layer, second performance — Basket's wire
// format stops HERE. Checkout sees Ordering's CustomerBasket and nothing else.
public sealed class HttpCustomerBasket(HttpClient httpClient) : ICustomerBasket
{
    // why: a private copy of the remote shape — a rename over there is
    // absorbed in this file, never in the domain.
    private sealed record WireBasket(Guid CustomerId, List<WireItem> Items);
    private sealed record WireItem(Guid ProductId, int Quantity);

    public async Task<CustomerBasket> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var wire = await httpClient.GetFromJsonAsync<WireBasket>(
            $"/internal/baskets/{customerId}", cancellationToken)
             ?? throw new InvalidOperationException("Basket service returned nothing.");

        return new CustomerBasket(
            wire.CustomerId,
            [.. wire.Items.Select(i => new CustomerBasketLine(i.ProductId, i.Quantity))]);
    }

    public async Task ClearAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/internal/baskets/{customerId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
