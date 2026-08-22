using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sphere.Catalog.Contracts;
using Sphere.Catalog.Features.Categories;
using Sphere.Catalog.Features.Products;
using Sphere.Catalog.Validation;

namespace Sphere.Catalog;

public static class CatalogModule
{
    public static IHostApplicationBuilder AddCatalogModule(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("catalog")
            ?? throw new InvalidOperationException("Connection string 'catalog' is missing.");

        builder.Services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddValidatorsFromAssemblyContaining<CatalogDbContext>(includeInternalTypes: true);
        builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "catalog-db");
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IProductPriceReader, ProductPriceReader>();

        return builder;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/products", GetProducts.HandleAsync);
        api.MapGet("/products/{id:guid}", GetProductById.HandleAsync);
        api.MapGet("/products/scroll", ScrollProducts.HandleAsync);
        api.MapPost("/products", CreateProduct.HandleAsync)
            .AddEndpointFilter<ValidationFilter<CreateProduct.Request>>();

        api.MapGet("/categories", GetCategories.HandleAsync);

        return app;
    }

    public static async Task SeedCatalogAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();
        await CatalogSeeder.SeedAsync(dbContext, scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }
}
