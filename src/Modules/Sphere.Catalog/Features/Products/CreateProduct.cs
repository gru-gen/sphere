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

    internal static async Task<Results<Created<ProductResponse>, Conflict<string>>> Handle(
        Request request, CatalogDbContext dbContext, TimeProvider clock, CancellationToken cancellationToken)
    {
        var skuTaken = await dbContext.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken);
        if (skuTaken)
        {
            return TypedResults.Conflict($"Sku '{request.Sku}' is already used.");
        }

        var product = ToProduct(request, clock);
        dbContext.Products.Add(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // why: the unique index is the REAL guard; when the race happens, it
            // speaks SQLSTATE 23505 and the API translates that into a calm 409.
            return TypedResults.Conflict($"Sku '{request.Sku}' is already used.");
        }

        return TypedResults.Created($"/api/products/{product.Id}", product.ToResponse());
    }

    internal static Product ToProduct(Request request, TimeProvider clock) => new()
    {
        Id = Guid.CreateVersion7(),
        Sku = request.Sku,
        Name = request.Name,
        Price = request.Price,
        CategoryId = request.CategoryId,
        CreatedAtUtc = clock.GetUtcNow(),
    };
}
