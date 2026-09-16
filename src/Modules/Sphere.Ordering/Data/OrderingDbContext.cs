namespace Sphere.Ordering.Data;

// summary: the module's context, plus the domain-event dispatch at save time.
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
        // why: dispatch BEFORE save — handlers write into THIS context, so their rows
        // (the history trail) commit in the same transaction as the change itself.
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.PullEvents())
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }

        // tradeoff: a handler that did external I/O here would do it inside a database
        // transaction. History is a row, so this is right; slow I/O belongs after commit.
        return await SaveChangesAsync(cancellationToken);
    }
}
