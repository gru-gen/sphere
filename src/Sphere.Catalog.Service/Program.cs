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

// why: only the standalone service opens the internal door. The gateway has
// no route for /internal, so this surface is invisible from outside.
app.MapInternalCatalogEndpoints();

app.Run();
