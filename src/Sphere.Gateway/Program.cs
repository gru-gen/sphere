using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("catalog-10s", policy => policy.Expire(TimeSpan.FromSeconds(10)));
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseOutputCache();

app.MapReverseProxy();

app.Run();

// summary: a named type so tests can point WebApplicationFactory at THIS host
// without clashing with the other hosts' Program classes.
public sealed class GatewayMarker;
