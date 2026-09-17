using Sphere.Ordering;
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.AddOrderingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await app.MigrateOrderingAsync();
    await app.EnsureOrderingTopicsAsync();
}

app.MapDefaultEndpoints();
app.MapOrderingEndpoints();

app.Run();

public sealed class NotificationServiceMarker;
