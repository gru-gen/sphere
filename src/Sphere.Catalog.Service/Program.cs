using Sphere.Catalog;
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.AddCatalogModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await app.SeedCatalogAsync();
}

app.MapDefaultEndpoints();

app.MapCatalogEndpoints();
app.MapInternalCatalogEndpoints();

app.Run();

public sealed class CatalogServiceMarker;
