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
using Sphere.Ordering.Features;

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

        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<OrderingDbContext>();
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        builder.Services.AddExceptionHandler<ValidationProblemHandler>();
        builder.Services.AddExceptionHandler<DomainProblemHandler>();

        // why: Dapper maps snake_case columns onto record properties.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        return builder;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/checkout",
            async Task<Created<CheckoutResult>> (CheckoutCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/orders/{result.OrderId}", result);
            });

        app.MapPost("/api/orders/{id:guid}/cancel",
            async Task<NoContent> (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new CancelOrderCommand(id), cancellationToken);
                return TypedResults.NoContent();
            });

        app.MapGet("/api/orders/{id:guid}", GetOrder.Handle);
        app.MapGet("/api/orders", ListOrders.Handle);

        return app;
    }

    public static async Task MigrateOrderingAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
