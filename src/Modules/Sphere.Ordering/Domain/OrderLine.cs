namespace Sphere.Ordering.Domain;

// summary: a snapshot line — the product's name and price AS THEY WERE at checkout.
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

    private OrderLine() { }  // why: EF Core materializes through this.

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
}
