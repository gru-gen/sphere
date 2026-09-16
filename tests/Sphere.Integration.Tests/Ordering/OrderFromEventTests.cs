using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Integration.Tests.Catalog;
using Sphere.Ordering.Application.PlaceOrder;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests.Ordering;

[Collection("postgres")]
public sealed class OrderFromEventTests : IDisposable
{
    private readonly PostgresFixture _postgresFixture;
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly OrderingServiceFactory _orderingServiceFactory;

    public OrderFromEventTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _catalogServiceFactory = new CatalogServiceFactory(_postgresFixture);
        _orderingServiceFactory = new OrderingServiceFactory(postgresFixture, _catalogServiceFactory.CreateClient());
    }

    public void Dispose()
    {
        _orderingServiceFactory.Dispose();
        _catalogServiceFactory.Dispose();
    }

    private async Task<Guid> AnyProductAsync()
    {
        var browse = await _catalogServiceFactory.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        return browse.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    private async Task SendAsync(PlaceOrderCommand command)
    {
        await using var scope = _orderingServiceFactory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    [Fact]
    public async Task The_same_event_processed_twice_creates_one_order()
    {
        var command = new PlaceOrderCommand(
            EventId: Guid.CreateVersion7(),
            OrderId: Guid.CreateVersion7(),
            CustomerId: Guid.CreateVersion7(),
            Lines: [new PlaceOrderLine(await AnyProductAsync(), 2)]);

        await SendAsync(command);
        // why: the redelivery — same eventId, same everything. The inbox
        // answers with silence: no exception, no second order, no history row.
        await SendAsync(command);

        await using var db = _postgresFixture.CreateOrderingContext();
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines)
            .SingleAsync(o => o.CustomerId == command.CustomerId);
        Assert.Equal(command.OrderId, order.Id);
        Assert.Equal(1, await db.History.CountAsync(h => h.OrderId == order.Id));
    }

    [Fact]
    public async Task Two_racers_one_event_one_order()
    {
        var command = new PlaceOrderCommand(
            EventId: Guid.CreateVersion7(),
            OrderId: Guid.CreateVersion7(),
            CustomerId: Guid.CreateVersion7(),
            Lines: [new PlaceOrderLine(await AnyProductAsync(), 1)]);

        await Task.WhenAll(SendAsync(command), SendAsync(command));

        await using var db = _postgresFixture.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == command.CustomerId));
    }
}
