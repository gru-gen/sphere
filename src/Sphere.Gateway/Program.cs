using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapReverseProxy();

app.Run();

// summary: a named type so tests can point WebApplicationFactory at THIS host
// without clashing with the other hosts' Program classes.
public sealed class GatewayMarker;
