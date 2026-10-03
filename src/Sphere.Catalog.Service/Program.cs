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

// why: only the standalone service opens the internal door. The gateway has
// no route for /internal, so this surface is invisible from outside.
app.MapInternalCatalogEndpoints();

app.Run();
