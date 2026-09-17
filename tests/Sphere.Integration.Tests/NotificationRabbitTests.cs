using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Sphere.Notification.Service.Messages;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class NotificationRabbitTests(PostgresFixture postgres, RabbitFixture rabbit)
    : IClassFixture<RabbitFixture>
{
    [Fact]
    public async Task One_command_over_the_wire_becomes_three_rows()
    {
        using var factory = new NotificationServiceFactory(postgres, rabbit.Host, rabbit.Port);
        factory.CreateClient();

        var orderId = Guid.CreateVersion7();
        var sender = factory.Services.GetRequiredService<ISendEndpointProvider>();
        var endpoint = await sender.GetSendEndpoint(new Uri("queue:send-order-confirmation"));
        await endpoint.Send(new SendOrderConfirmation(
            orderId, Guid.CreateVersion7(), 120.00m, "EUR"));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = postgres.CreateNotificationContext();
            if (await db.Notifications.CountAsync(n => n.OrderId == orderId) == 3)
            {
                return;
            }
            await Task.Delay(250);
        }
        Assert.Fail("The command never became three channel rows.");
    }
}
