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
}

app.MapDefaultEndpoints();
app.MapBasketEndpoints();

// why: only the standalone service opens the internal door — checkout's
// surface, invisible from outside the network.
app.MapInternalBasketEndpoints();

app.Run();
