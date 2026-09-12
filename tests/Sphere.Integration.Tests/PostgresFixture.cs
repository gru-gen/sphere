using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sphere.Basket.Data;
using Sphere.Catalog.Data;
using Sphere.Ordering.Data;
using Testcontainers.PostgreSql;

namespace Sphere.Integration.Tests;

// summary: ONE real PostgreSQL for the whole collection — the production pin,
// started by Testcontainers, migrated once, shared by every test class.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17.5-alpine")
        .WithDatabase("sphere")
        .WithUsername("sphere")
        .WithPassword("sphere-dev")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public string CatalogConnectionString =>
        ConnectionString.Replace("Database=sphere", "Database=catalog_db");

    public string BasketConnectionString =>
        ConnectionString.Replace("Database=sphere", "Database=basket_db");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var admin = new NpgsqlConnection(ConnectionString))
        {
            await admin.OpenAsync();
            foreach (var name in new[] { "catalog_db", "basket_db" })
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
                await create.ExecuteNonQueryAsync();
            }
        }

        // why: apply every hand-written migration to an EMPTY database — if any
        // of them drifted from the model, the whole suite fails right here.
        await using var catalog = CreateCatalogContext();
        await catalog.Database.MigrateAsync();

        await using var basket = CreateBasketContext();
        await basket.Database.MigrateAsync();

        await using var ordering = CreateOrderingContext();
        await ordering.Database.MigrateAsync();
    }

    internal CatalogDbContext CreateCatalogContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(CatalogConnectionString).Options);

    internal BasketDbContext CreateBasketContext() =>
        new(new DbContextOptionsBuilder<BasketDbContext>()
            .UseNpgsql(BasketConnectionString).Options);

    internal OrderingDbContext CreateOrderingContext(IPublisher? publisher = null) =>
        new(Options<OrderingDbContext>(), publisher ?? new NoopPublisher());

    private DbContextOptions<T> Options<T>() where T : DbContext =>
        new DbContextOptionsBuilder<T>().UseNpgsql(ConnectionString).Options;

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    // summary: a publisher that swallows events — for data-layer tests that are
    // not about the pipeline. Pipeline tests wire the real MediatR instead.
    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
