using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sphere.Ordering.Application.Behaviors;
using Sphere.Ordering.Application.CancelOrder;
using Sphere.Ordering.Application.Checkout;
using Sphere.Ordering.Application.ExceptionHandlers;
using Sphere.Ordering.Features;
using Sphere.Ordering.Infrastructure;
using Sphere.Ordering.Validation;

namespace Sphere.Ordering;

public static class OrderingModule
{
    public static IHostApplicationBuilder AddOrderingModule(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("ordering")
            ?? throw new InvalidOperationException("Connection string 'ordering' is missing.");

        builder.Services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddSingleton(new OrderingReadDb(connectionString));
        builder.Services.AddValidatorsFromAssemblyContaining<OrderingDbContext>(includeInternalTypes: true);
        builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "ordering-db");

        // why: prices now live in another PROCESS. A typed client carries the
        // base address and the first timeout budget in one place.
        var catalogBaseUrl = builder.Configuration["Catalog:BaseUrl"]
            ?? throw new InvalidOperationException("Setting 'Catalog:BaseUrl' is missing.");
        builder.Services.AddHttpClient<IProductPriceReader, CatalogHttpPriceReader>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseUrl);
            // why: fail in 2 seconds, not in 100 — a hung checkout holds a
            // request thread AND a database connection (the pool math).
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        // why: the second consumer-owned port gets the same treatment — one
        // typed client, one base address, one 2-second budget.
        var basketBaseUrl = builder.Configuration["Basket:BaseUrl"]
            ?? throw new InvalidOperationException("Setting 'Basket:BaseUrl' is missing.");
        builder.Services.AddHttpClient<ICustomerBasket, HttpCustomerBasket>(client =>
        {
            client.BaseAddress = new Uri(basketBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderingDbContext>();
            // why: order matters — logging wraps validation wraps the handler.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        builder.Services.AddExceptionHandler<ValidationProblemHandler>();
        builder.Services.AddExceptionHandler<DomainProblemHandler>();

        // why: Dapper maps snake_case columns onto record properties.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/checkout",
            async Task<Created<CheckoutResult>> (CheckoutCommand command, HttpContext httpContext,
                ISender sender, CancellationToken cancellationToken) =>
            {
                // why: the key is TRANSPORT metadata, not business payload — it
                // arrives as a header and joins the command at the door.
                var key = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
                var result = await sender.Send(command with { IdempotencyKey = key }, cancellationToken);
                return TypedResults.Created($"/api/orders/{result.OrderId}", result);
            });

        app.MapPost("/api/orders/{id:guid}/cancel",
            async Task<NoContent> (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new CancelOrderCommand(id), cancellationToken);
                return TypedResults.NoContent();
            });

        app.MapGet("/api/orders/{id:guid}", GetOrder.Handle);
        app.MapPost("/api/orders", ListOrders.Handle)
            .AddEndpointFilter<ValidationFilter<ListOrders.Request>>();

        return app;
    }

    public static async Task MigrateOrderingAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderingDbContext>()
            .Database.MigrateAsync();
    }
}
