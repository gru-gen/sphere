namespace Sphere.Notification.Service.Messages;

public sealed record OrderConfirmationReady(
    Guid OrderId,
    Guid CustomerId,
    string Subject,
    string Body);
