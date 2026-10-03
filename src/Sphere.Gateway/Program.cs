// summary: the strangler facade — ONE public door that decides, per route,
// whether the monolith or a new service answers. Clients never notice a cut.
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

// why: the whole route table lives in configuration — moving a route is a
// config change and a restart, not a code change and a release.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();


app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapReverseProxy();

app.Run();
