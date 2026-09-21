using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace Sphere.Ordering.Features;

public sealed record OrderResponse(
    Guid Id, Guid CustomerId, string Status, decimal Total, string Currency,
    DateTimeOffset PlacedAtUtc, IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(
    Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record OrderSummaryResponse(
    Guid Id, string Status, decimal Total, string Currency, DateTimeOffset PlacedAtUtc);

// summary: the read side — plain SQL through Dapper, no change tracker, no aggregate.
internal sealed class OrderingReadDb(string connectionString)
{
    public NpgsqlConnection Open() => new(connectionString);
}

internal static class GetOrder
{
    private const string Sql = """
        select o.id, o.customer_id as CustomerId, o.status_id as StatusId,
               o.total, o.currency, o.placed_at_utc as PlacedAtUtc
        from ordering.orders o where o.id = @id;

        select l.product_id as ProductId, l.product_name as ProductName,
               l.unit_price as UnitPrice, l.quantity
        from ordering.order_lines l where l.order_id = @id
        order by l.product_name;
        """;

    internal static async Task<Results<Ok<OrderResponse>, NotFound>> Handle(
       Guid id, OrderingReadDb readDb, CancellationToken ct)
    {
        await using var connection = readDb.Open();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(Sql, new { id }, cancellationToken: ct));

        var head = await multi.ReadSingleOrDefaultAsync<OrderRow>();
        if (head is null)
        {
            return TypedResults.NotFound();
        }
        var lines = (await multi.ReadAsync<OrderLineResponse>()).ToList();

        // DB returns a DateTime for placed_at_utc; convert to DateTimeOffset (UTC)
        var placedAt = new DateTimeOffset(DateTime.SpecifyKind(head.PlacedAtUtc, DateTimeKind.Utc));
        return TypedResults.Ok(new OrderResponse(
            head.Id, head.CustomerId, OrderStatus.FromId(head.StatusId).Name,
            head.Total, head.Currency, placedAt, lines));
    }

    // Use DateTime here so Dapper can materialize the row; convert to DateTimeOffset above
    private sealed record OrderRow(
       Guid Id, Guid CustomerId, int StatusId, decimal Total,
       string Currency, DateTime PlacedAtUtc);
}

internal static class ListOrders
{
    internal sealed record Request(Guid CustomerId);

    private const string Sql = """
        select o.id, o.status_id as StatusId, o.total, o.currency,
               o.placed_at_utc as PlacedAtUtc
        from ordering.orders o
        where o.customer_id = @customerId
        order by o.placed_at_utc desc
        limit 50;
        """;

    internal static async Task<Ok<List<OrderSummaryResponse>>> Handle(
        Request request, OrderingReadDb readDb, CancellationToken ct)
    {
        await using var connection = readDb.Open();
        var rows = await connection.QueryAsync<SummaryRow>(
            new CommandDefinition(Sql, new { request.CustomerId }, cancellationToken: ct));

        return TypedResults.Ok(rows
            .Select(r => new OrderSummaryResponse(
                r.Id, OrderStatus.FromId(r.StatusId).Name, r.Total, r.Currency,
                new DateTimeOffset(DateTime.SpecifyKind(r.PlacedAtUtc, DateTimeKind.Utc))))
            .ToList());
    }

    private sealed record SummaryRow(
        Guid Id, int StatusId, decimal Total, string Currency, DateTime PlacedAtUtc);
}
