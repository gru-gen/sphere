using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Sphere.ServiceDefaults;

// summary: the chassis — everything EVERY Sphere service needs, wired
// once: telemetry, resilient outbound HTTP, uniform health endpoints.
// tradeoff: a chassis change ships with every service's next deploy; the
// price of one shared library is that it must stay small and boring.
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();

        // why: EVERY outbound HttpClient in the process gets the standard
        // resilience pipeline (retries, circuit breaker, timeouts) as a
        // DEFAULT.the default is chassis business.
        // A stricter per-client budget still wins.
        builder.Services.ConfigureHttpClientDefaults(
            http => http.AddStandardResilienceHandler());

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        // why: exporters follow the standard environment convention — set
        // OTEL_EXPORTER_OTLP_ENDPOINT and telemetry flows there; leave it
        // unset and the pipelines stay quiet. The collector that receives
        // it arrives in a later part; the wiring is ready today.
        if (!string.IsNullOrEmpty(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // why: identical probe paths on every service — humans and machines
        // never have to remember who spells health how.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
