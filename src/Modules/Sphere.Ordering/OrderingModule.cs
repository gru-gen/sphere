using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sphere.Ordering.Application.Behaviors;
using Sphere.Ordering.Application.CancelOrder;
using Sphere.Ordering.Application.Events;
using Sphere.Ordering.Application.Notifications;
using Sphere.Ordering.Application.Pricing;
using Sphere.Ordering.Features;
using Sphere.Ordering.Infrastructure;

namespace Sphere.Ordering;

public static class OrderingModule
{
    public static IHostApplicationBuilder AddOrderingModule(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("ordering")
            ?? throw new InvalidOperationException("Connection string 'ordering' is missing.");

        builder.Services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddSingleton(new OrderingReadDb(connectionString));
        builder.Services.AddValidatorsFromAssemblyContaining<OrderingDbContext>(includeInternalTypes: true);
        builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "ordering-db");
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<OrderingDbContext>();
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        var catalogBaseUrl = builder.Configuration["Catalog:BaseUrl"]
            ?? throw new InvalidOperationException("Setting 'Catalog:BaseUrl' is missing.");
        builder.Services.AddHttpClient<IProductPriceReader, CatalogHttpPriceReader>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseUrl);
            // why: fail in 2 seconds, not in 100 — a hung checkout holds a
            // request thread AND a database connection.
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Setting 'Kafka:BootstrapServers' is missing.");
        builder.Services.AddSingleton(new KafkaSettings(bootstrapServers));
        builder.Services.AddHostedService<BasketCheckedOutConsumer>();

        var source = builder.Configuration["Kafka:Source"]
                ?? throw new InvalidOperationException("Setting 'Kafka:Source' is missing.");
        builder.Services.AddSingleton<IOrderingEvents>(sp => new KafkaOrderingEvents(
            bootstrapServers,
            source,
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<KafkaOrderingEvents>>()));

        var rabbit = new RabbitConnectionSettings(
            builder.Configuration["Rabbit:Host"]
                ?? throw new InvalidOperationException("Setting 'Rabbit:Host' is missing."),
            ushort.Parse(builder.Configuration["Rabbit:Port"]
                ?? throw new InvalidOperationException("Setting 'Rabbit:Port' is missing.")),
            builder.Configuration["Rabbit:User"]
                ?? throw new InvalidOperationException("Setting 'Rabbit:User' is missing."),
            builder.Configuration["Rabbit:Pass"]
                ?? throw new InvalidOperationException("Setting 'Rabbit:Pass' is missing."));
        builder.Services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, rabbit.Port, "/", h =>
                {
                    h.Username(rabbit.User);
                    h.Password(rabbit.Password);
                });
            });
        });

        builder.Services.AddScoped<IOrderNotifier, MassTransitOrderNotifier>();

        builder.Services.AddExceptionHandler<ValidationProblemHandler>();
        builder.Services.AddExceptionHandler<DomainProblemHandler>();

        // why: Dapper maps snake_case columns onto record properties.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        return builder;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{id:guid}/cancel",
            async Task<NoContent> (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new CancelOrderCommand(id), cancellationToken);
                return TypedResults.NoContent();
            });

        app.MapGet("/api/orders/{id:guid}", GetOrder.Handle);
        app.MapGet("/api/orders/customer/{customerId:guid}", ListOrders.Handle);

        return app;
    }

    public static async Task EnsureOrderingTopicsAsync(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<KafkaSettings>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("OrderingTopics");
        using var admin = new AdminClientBuilder(
            new AdminClientConfig
            {
                BootstrapServers = settings.BootstrapServers
            }).Build();

        try
        {
            await admin.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = BasketCheckedOutConsumer.RetryTopic,
                        NumPartitions = 1,
                        ReplicationFactor = 1,
                    },
                    new TopicSpecification
                    {
                        Name = BasketCheckedOutConsumer.DeadLetterTopic,
                        NumPartitions = 1,
                        ReplicationFactor = 1,
                    }
                ],
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(3) });
        }
        catch (CreateTopicsException e) when(
            e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // the second start of the same stack — nothing to do.
        }
        catch (KafkaException e)
        {
            logger.LogWarning(e, "Ordering topics were not ensured.");
        }
    }

    public static async Task MigrateOrderingAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
