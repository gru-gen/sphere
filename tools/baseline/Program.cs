// The probe: the smallest possible HTTP endpoint.
// It measures the MACHINE, not an application.
var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("/ping", () => "pong");

app.Run();
