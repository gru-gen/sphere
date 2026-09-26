using Sphere.Basket;
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.AddBasketModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await app.MigrateBasketAsync();
    await app.EnsureBasketTopicAsync();
}

app.MapDefaultEndpoints();
app.MapBasketEndpoints();

app.Run();

// summary: a named type so tests can point WebApplicationFactory at THIS host
// without clashing with the other hosts' Program classes.
public sealed class BasketServiceMarker;
