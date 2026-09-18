using Json.Schema;
using Sphere.Basket.Contracts;
using Sphere.Ordering.Contracts.Events;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sphere.Integration.Tests;

public sealed class SchemaContractTests
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private static JsonSchema LoadSchema(string name) => JsonSchema.FromText(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contracts", name)));

    [Fact]
    public void The_event_we_publish_satisfies_the_contract()
    {
        var schema = LoadSchema("basket-checked-out.v1.json");
        var evt = new BasketCheckedOut(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            [new BasketCheckedOutLine(Guid.CreateVersion7(), 2)],
            DateTimeOffset.UtcNow);

        var result = schema.Evaluate(
            JsonNode.Parse(JsonSerializer.Serialize(evt, Json)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void The_order_placed_event_satisfies_its_contract()
    {
        var schema = LoadSchema("order-placed.v1.json");
        var evt = new OrderPlaced(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            59.90m, "EUR", DateTimeOffset.UtcNow);

        var result = schema.Evaluate(
            JsonNode.Parse(JsonSerializer.Serialize(evt, Json)));

        Assert.True(result.IsValid);
    }


    [Fact]
    public void A_payload_missing_the_customer_fails_the_contract()
    {
        var schema = LoadSchema("basket-checked-out.v1.json");
        // why: the exact break the registry exists to refuse - a "small
        // cleanup" that removes a field old readers require.
        var broken = """
            { "eventId": "0d9f9c50-0000-7000-8000-000000000001",
              "checkoutId": "0d9f9c50-0000-7000-8000-000000000002",
              "lines": [ { "productId": "0d9f9c50-0000-7000-8000-000000000003", "quantity": 1 } ],
              "checkedOutAtUtc": "2026-08-12T10:00:00Z" }
            """;

        var result = schema.Evaluate(JsonNode.Parse(broken));

        Assert.False(result.IsValid);
    }
}
