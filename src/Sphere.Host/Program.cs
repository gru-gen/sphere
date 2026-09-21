using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Sphere.Basket;
using Sphere.Catalog;
using Sphere.Ordering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.AddCatalogModule();
builder.AddBasketModule();
builder.AddOrderingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.SeedCatalogAsync();
    await app.MigrateBasketAsync();
    await app.MigrateOrderingAsync();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapCatalogEndpoints();
app.MapBasketEndpoints();
app.MapOrderingEndpoints();

app.Run();
