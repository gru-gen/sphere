namespace Sphere.Ordering.Domain;

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
