using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Sphere.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.AddCatalogModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await app.SeedCatalogAsync();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapCatalogEndpoints();
app.MapInternalCatalogEndpoints();

app.Run();

public sealed class CatalogServiceMarker;
