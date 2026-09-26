using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Integration.Tests.Catalog;
using Sphere.Ordering.Application.PlaceOrder;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class OrderFromEventTests : IDisposable
{
    private readonly PostgresContainer _fixture;
    private readonly CatalogServiceFactory _catalog;
    private readonly OrderingServiceFactory _ordering;

    public OrderFromEventTests(PostgresContainer fixture)
    {
        _fixture = fixture;
        _catalog = new CatalogServiceFactory(fixture);
        _ordering = new OrderingServiceFactory(fixture, _catalog.CreateClient());
    }

    public void Dispose()
    {
        _ordering.Dispose();
        _catalog.Dispose();
    }

    private async Task<Guid> AnyProductAsync()
    {
        var browse = await _catalog.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        return browse.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    private async Task SendAsync(PlaceOrderCommand command)
    {
        await using var scope = _ordering.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    [Fact]
    public async Task The_same_event_processed_twice_creates_one_order()
    {
        var command = new PlaceOrderCommand(
            EventId: Guid.CreateVersion7(),
            OrderId: Guid.CreateVersion7(),
            CustomerId: Guid.CreateVersion7(),
            Items: [new PlaceOrderItem(await AnyProductAsync(), 2)]);

        await SendAsync(command);
        // why: the redelivery — same eventId, same everything. The inbox
        // answers with silence: no exception, no second order, no history row.
        await SendAsync(command);

        await using var db = _fixture.CreateOrderingContext();
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines)
            .SingleAsync(o => o.CustomerId == command.CustomerId);
        Assert.Equal(command.OrderId, order.Id);
        Assert.Equal(1, await db.OrderHistory.CountAsync(h => h.OrderId == order.Id));
    }

    [Fact]
    public async Task Two_racers_one_event_one_order()
    {
        // why: after a rebalance, two group members can hold the same fact
        // for a moment — the inbox's primary key is the fence between them.
        var command = new PlaceOrderCommand(
            EventId: Guid.CreateVersion7(),
            OrderId: Guid.CreateVersion7(),
            CustomerId: Guid.CreateVersion7(),
            Items: [new PlaceOrderItem(await AnyProductAsync(), 1)]);

        await Task.WhenAll(SendAsync(command), SendAsync(command));

        await using var db = _fixture.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == command.CustomerId));
    }
}
