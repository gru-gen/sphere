using Scalar.AspNetCore;
using Sphere.Basket;
using Sphere.Ordering;
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.AddBasketModule();
builder.AddOrderingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.MigrateBasketAsync();
    await app.MigrateOrderingAsync();
}

app.MapDefaultEndpoints();

app.MapBasketEndpoints();
app.MapOrderingEndpoints();

app.Run();

// summary: gives WebApplicationFactory a public type to point at (top-level
// statements make Program internal by default).
public sealed class HostMarker;
