using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Basket.Contracts;
using Sphere.Catalog.Contracts;
using Sphere.Ordering.Application.Behaviors;
using Sphere.Ordering.Application.CancelOrder;
using Sphere.Ordering.Application.Checkout;
using Sphere.Ordering.Data;
using Sphere.Ordering.Domain.Abstract;

namespace Sphere.Integration.Tests;

// summary: the money test — the REAL MediatR pipeline and the REAL database;
// only the sibling modules are stubbed at their public contracts.
[Collection("postgres")]
public class CheckoutPipelineTests(PostgresFixture postgresFixture)
{
    [Fact]
    public async Task Checkout_commits_order_lines_and_history_atomically_then_clears()
    {
        var basket = new StubBasket(
            new BasketSnapshotItem(Guid.CreateVersion7(), 2),
            new BasketSnapshotItem(Guid.CreateVersion7(), 1));

        await using var provider = BuildPipeline(basket);
        var customerId = Guid.CreateVersion7();

        CheckoutResult result;
        await using (var scope = provider.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            result = await sender.Send(new CheckoutCommand(customerId));
        }

        await using var db = postgresFixture.CreateOrderingContext();
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Lines).SingleAsync(o => o.Id == result.OrderId);

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(35m, order.Total);
        Assert.Equal(1, await db.History.CountAsync(h => h.OrderId == order.Id));
        Assert.True(basket.Cleared, "the basket clear is the SECOND commit");
    }

    [Fact]
    public async Task Cancelling_twice_is_refused_by_the_domain()
    {
        var basket = new StubBasket(new BasketSnapshotItem(Guid.CreateVersion7(), 1));
        await using var provider = BuildPipeline(basket);

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var placed = await sender.Send(new CheckoutCommand(Guid.CreateVersion7()));

        await sender.Send(new CancelOrderCommand(placed.OrderId));
        await Assert.ThrowsAsync<DomainException>(
            () => sender.Send(new CancelOrderCommand(placed.OrderId)));

        await using var db = postgresFixture.CreateOrderingContext();
        Assert.Equal(2, await db.History.CountAsync(h => h.OrderId == placed.OrderId));
    }

    private ServiceProvider BuildPipeline(StubBasket basket)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(postgresFixture.ConnectionString));
        services.AddValidatorsFromAssemblyContaining<OrderingDbContext>(includeInternalTypes: true);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderingDbContext>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddSingleton<IBasketStore>(basket);
        services.AddSingleton<IProductPriceReader>(new StubPrices());
        return services.BuildServiceProvider();
    }

    private sealed class StubBasket(params BasketSnapshotItem[] items) : IBasketStore
    {
        public bool Cleared { get; private set; }

        public Task<BasketSnapshot> GetAsync(Guid customerId, CancellationToken cancellationToken)
            => Task.FromResult(new BasketSnapshot(customerId, items));

        public Task ClearAsync(Guid customerId, CancellationToken cancellationToken)
        {
            Cleared = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPrices : IProductPriceReader
    {
        public Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
            IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
        {
            // why: deterministic prices — first product 10, next 15, then 20...
            var map = productIds.Select((id, i) => new ProductPrice(id, $"Stub {i}", 10m + 5m * i))
                .ToDictionary(p => p.ProductId);
            return Task.FromResult<IReadOnlyDictionary<Guid, ProductPrice>>(map);
        }
    }
}
