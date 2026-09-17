using MassTransit;
using Sphere.Notification.Service.Consumers;
using Sphere.Notification.Service.Infrastructure;
using Sphere.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("notification")
    ?? throw new InvalidOperationException("Connection string 'notification' is missing.");
builder.Services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(connectionString));

var rabbit = new RabbitSettings(
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
    x.AddConsumer<SendOrderConfirmationConsumer>();
    x.AddConsumer<EmailConsumer>();
    x.AddConsumer<SmsConsumer>();
    x.AddConsumer<PushConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.Port, "/", h =>
        {
            h.Username(rabbit.User);
            h.Password(rabbit.Pass);
        });

        cfg.ReceiveEndpoint("send-order-confirmation", e =>
        {
            e.ConfigureConsumer<SendOrderConfirmationConsumer>(context);
        });

        cfg.ReceiveEndpoint("notify-email", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
            e.ConfigureConsumer<EmailConsumer>(context);
        });
        cfg.ReceiveEndpoint("notify-sms", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
            e.ConfigureConsumer<SmsConsumer>(context);
        });
        cfg.ReceiveEndpoint("notify-push", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
            e.ConfigureConsumer<PushConsumer>(context);
        });
    });
});

var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? throw new InvalidOperationException("Setting 'Kafka:BootstrapServers' is missing.");
builder.Services.AddSingleton(new KafkaSettings(bootstrapServers));
builder.Services.AddHostedService<BasketCheckedOutLogger>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>()
        .Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.Run();

public sealed class OrderingServiceMarker;
