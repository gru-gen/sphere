using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace Sphere.Catalog.Features.Products;

internal static class CreateProduct
{
    internal sealed record Request(string Sku, string Name, decimal Price, Guid CategoryId);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(32).Matches("^[A-Z0-9-]+$");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Price).InclusiveBetween(0.01m, 100_000m);
            RuleFor(x => x.CategoryId).NotEmpty();
        }
    }

    internal static async Task<Results<Created<ProductResponse>, Conflict<string>>> HandleAsync(
        Request request, CatalogDbContext dbContext, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var skuTaken = await dbContext.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken);
        if (skuTaken)
        {
            return TypedResults.Conflict($"Sku '{request.Sku}' is already used.");
        }

        var product = ToProduct(request, timeProvider);
        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict($"Sku '{request.Sku}' is already used.");
        }

        return TypedResults.Created($"/api/products/{product.Id}", product.ToResponse());
    }

    internal static Product ToProduct(Request request, TimeProvider timeProvider) => new()
    {
        Id = Guid.CreateVersion7(),
        Sku = request.Sku,
        Name = request.Name,
        Price = request.Price,
        CategoryId = request.CategoryId,
        CreatedAtUtc = timeProvider.GetUtcNow(),
    };
}
