using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sphere.Basket.Contracts;
using Sphere.Basket.Features;
using Sphere.Basket.Validation;

namespace Sphere.Basket;

public static class BasketModule
{
    public static IHostApplicationBuilder AddBasketModule(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("basket")
            ?? throw new InvalidOperationException("Connection string 'basket' is missing.");

        builder.Services.AddDbContext<BasketDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddValidatorsFromAssemblyContaining<BasketDbContext>(includeInternalTypes: true);
        builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "basket-db");
        builder.Services.AddScoped<IBasketStore, BasketStore>();

        return builder;
    }

    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/basket");

        api.MapGet("/{customerId:guid}", GetBasket.HandleAsync);
        api.MapPost("/{customerId:guid}/items", AddItem.HandleAsync)
            .AddEndpointFilter<ValidationFilter<AddItem.Request>>();
        api.MapDelete("/{customerId:guid}/items/{productId:guid}", RemoveItem.HandleAsync);

        return app;
    }

    public static async Task MigrateBasketAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
