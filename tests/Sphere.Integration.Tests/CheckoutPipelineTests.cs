using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Ordering.Application.Cancel;
using Sphere.Ordering.Application.Checkout;
using Sphere.Ordering.Behaviors;
using Sphere.Ordering.Data;
using Sphere.Ordering.Domain.Abstract;

namespace Sphere.Integration.Tests;

// summary: the money test — the REAL MediatR pipeline and the REAL database;
// only the sibling modules are stubbed at their public contracts.
[Collection("postgres")]
public class CheckoutPipelineTests(PostgresContainer postgresContainer)
{
    [Fact]
    public async Task Checkout_commits_order_lines_and_history_atomically_then_clears()
    {
        var basket = new StubBasket(
            new CustomerBasketItem(Guid.CreateVersion7(), 2),
            new CustomerBasketItem(Guid.CreateVersion7(), 1));
        await using var provider = BuildPipeline(basket);
        var customerId = Guid.CreateVersion7();

        CheckoutResult result;
        await using (var scope = provider.CreateAsyncScope())
        {
            result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CheckoutCommand(customerId));
        }

        await using var db = postgresContainer.CreateOrderingContext();
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Lines).SingleAsync(o => o.Id == result.OrderId);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(35m, order.Total);
        Assert.Equal(1, await db.OrderHistory.CountAsync(h => h.OrderId == order.Id));
        Assert.True(basket.Cleared, "the basket clear is the SECOND commit");
    }

    [Fact]
    public async Task Cancelling_twice_is_refused_by_the_domain()
    {
        var basket = new StubBasket(new CustomerBasketItem(Guid.CreateVersion7(), 1));
        await using var provider = BuildPipeline(basket);

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var placed = await sender.Send(new CheckoutCommand(Guid.CreateVersion7()));

        await sender.Send(new CancelOrderCommand(placed.OrderId));
        await Assert.ThrowsAsync<DomainException>(
            () => sender.Send(new CancelOrderCommand(placed.OrderId)));

        await using var db = postgresContainer.CreateOrderingContext();
        Assert.Equal(2, await db.OrderHistory.CountAsync(h => h.OrderId == placed.OrderId));
    }

    [Fact]
    public async Task Two_racers_one_key_one_order()
    {
        var basket = new StubBasket(new CustomerBasketItem(Guid.CreateVersion7(), 1));
        await using var provider = BuildPipeline(basket);
        var customerId = Guid.CreateVersion7();
        // why: the stub basket never empties — the race under test is the KEY
        // fence, isolated from the basket-clearing race a real retry heals.
        var command = new CheckoutCommand(customerId, $"race-{customerId}");

        async Task<CheckoutResult> RunAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(command);
        }

        var results = await Task.WhenAll(RunAsync(), RunAsync());

        Assert.Equal(results[0].OrderId, results[1].OrderId);
        await using var db = postgresContainer.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == customerId));
    }

    private ServiceProvider BuildPipeline(StubBasket basket)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(postgresContainer.ConnectionString));
        services.AddValidatorsFromAssemblyContaining<OrderingDbContext>(
            includeInternalTypes: true);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderingDbContext>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBeahvior<,>));
        });
        services.AddSingleton<ICustomerBasket>(basket);
        services.AddSingleton<IProductPriceReader>(new StubPrices());
        return services.BuildServiceProvider();
    }

    private sealed class StubBasket(params CustomerBasketItem[] lines) : ICustomerBasket
    {
        public bool Cleared { get; private set; }

        public Task<CustomerBasket> GetAsync(Guid customerId, CancellationToken ct)
            => Task.FromResult(new CustomerBasket(customerId, lines));

        public Task ClearAsync(Guid customerId, CancellationToken ct)
        {
            Cleared = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPrices : IProductPriceReader
    {
        public Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken ct)
        {
            var map = ids.Select((id, i) => new ProductPrice(id, $"Stub {i}", 10m + 5m * i))
                .ToDictionary(p => p.ProductId);
            return Task.FromResult<IReadOnlyDictionary<Guid, ProductPrice>>(map);
        }
    }
}
