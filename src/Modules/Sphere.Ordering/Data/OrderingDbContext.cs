namespace Sphere.Ordering.Data;

internal sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options, IPublisher publisher)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderHistoryEntry> History => Set<OrderHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }

    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken)
    {
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.PullDomainEvents())
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }

        return await SaveChangesAsync(cancellationToken);
    }
}
