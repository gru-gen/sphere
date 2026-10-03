// summary: the strangler facade — ONE public door that decides, per route,
// whether the monolith or a new service answers. Clients never notice a cut.
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// why: the whole route table lives in configuration — moving a route is a
// config change and a restart, not a code change and a release.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// why: ten seconds of shared memory at the edge absorbs read storms the
// catalog never sees. The price is honesty: a product edit can be up to ten
// seconds late at the front door — chosen, written down, revisited when a
// real cache tier arrives.
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("catalog-10s", policy => policy.Expire(TimeSpan.FromSeconds(10)));
});

var app = builder.Build();

app.UseOutputCache();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
