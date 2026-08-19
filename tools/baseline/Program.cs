// It measures the MACHINE, not an application.
var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.Run();
