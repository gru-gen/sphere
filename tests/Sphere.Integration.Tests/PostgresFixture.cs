using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sphere.Basket.Data;
using Sphere.Catalog.Data;
using Sphere.Notification.Service.Data;
using Sphere.Ordering.Data;
using Testcontainers.PostgreSql;

namespace Sphere.Integration.Tests;

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

    public string OrderingConnectionString =>
        ConnectionString.Replace("Database=sphere", "Database=ordering_db");

    public string NotificationConnectionString =>
        ConnectionString.Replace("Database=sphere", "Database=notification_db");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var admin = new NpgsqlConnection(ConnectionString))
        {
            await admin.OpenAsync();
            // why: the three service-owned databases, exactly like development.
            foreach (var name in new[] { "catalog_db", "basket_db", "ordering_db", "notification_db" })
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
                await create.ExecuteNonQueryAsync();
            }
        }

        await using var catalog = CreateCatalogContext();
        await catalog.Database.MigrateAsync();
        await using var basket = CreateBasketContext();
        await basket.Database.MigrateAsync();
        await using var ordering = CreateOrderingContext();
        await ordering.Database.MigrateAsync();
        await using var notification = CreateNotificationContext();
        await notification.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    internal CatalogDbContext CreateCatalogContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(CatalogConnectionString).Options);

    internal BasketDbContext CreateBasketContext() =>
        new(new DbContextOptionsBuilder<BasketDbContext>()
            .UseNpgsql(BasketConnectionString).Options);

    internal OrderingDbContext CreateOrderingContext(IPublisher? publisher = null) =>
        new(new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(OrderingConnectionString).Options,
            publisher ?? new NoopPublisher());

    internal NotificationDbContext CreateNotificationContext() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(NotificationConnectionString).Options);

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification,
            CancellationToken ct = default) where TNotification : INotification
            => Task.CompletedTask;
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

