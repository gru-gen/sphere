using Sphere.Ordering.Application.Checkout;
using System.Net.Http.Json;

namespace Sphere.Ordering.Infrastructure;

public sealed class HttpCustomerBasket(HttpClient http) : ICustomerBasket
{
    private sealed record WireBasket(Guid CustomerId, List<WireItem> Items);
    private sealed record WireItem(Guid ProductId, int Quantity);

    public async Task<CustomerBasket> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var wire = await http.GetFromJsonAsync<WireBasket>(
            $"/internal/baskets/{customerId}", cancellationToken)
              ?? throw new InvalidOperationException("Basket service returned nothing.");

        return new CustomerBasket(
            wire.CustomerId,
            [.. wire.Items.Select(i => new CustomerBasketItem(i.ProductId, i.Quantity))]);
    }

    public async Task ClearAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var response = await http.DeleteAsync($"/internal/baskets/{customerId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
