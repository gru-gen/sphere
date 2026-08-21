namespace Sphere.Ordering.Domain;

internal sealed class OrderLine
{
    internal OrderLine(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    private OrderLine() { }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
}
