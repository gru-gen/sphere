namespace Sphere.Ordering.Domain;

// summary: an enumeration class — a fixed set of named values that can carry behavior,
// unlike a C# enum. Persisted as its integer id.
internal sealed class OrderStatus
{
    public static readonly OrderStatus Placed = new(1, nameof(Placed));
    public static readonly OrderStatus Cancelled = new(2, nameof(Cancelled));

    private OrderStatus(int id, string name) => (Id, Name) = (id, name);

    public int Id { get; }
    public string Name { get; }

    public static OrderStatus FromId(int id) => id switch
    {
        1 => Placed,
        2 => Cancelled,
        _ => throw new DomainException($"Unknown order status id {id}."),
    };
}
