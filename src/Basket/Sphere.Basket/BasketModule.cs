using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sphere.Basket.Contracts;
using Sphere.Basket.Features;
using Sphere.Basket.Infrastructure;
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
        builder.Services.AddSingleton(TimeProvider.System);

        var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Setting 'Kafka:BootstrapServers' is missing.");
        builder.Services.AddSingleton<IBasketEvents>(sp => new KafkaBasketEvents(
            bootstrapServers,
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<KafkaBasketEvents>>()));

        return builder;
    }

    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/basket");

        api.MapGet("/{customerId:guid}", GetBasket.Handle);
        api.MapPost("/{customerId:guid}/items", AddItem.Handle)
            .AddEndpointFilter<ValidationFilter<AddItem.Request>>();
        api.MapDelete("/{customerId:guid}/items/{productId:guid}", RemoveItem.Handle);

        app.MapPost("/api/checkout", Checkout.Handle)
            .AddEndpointFilter<ValidationFilter<Checkout.Request>>();

        return app;
    }

    public static async Task EnsureBasketTopicAsync(this WebApplication app)
    {
        await app.Services.GetRequiredService<IBasketEvents>().EnsureTopicAsync();
    }

    public static async Task MigrateBasketAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
