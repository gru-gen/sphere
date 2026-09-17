namespace Sphere.Notification.Service.Infrastructure;

public sealed record RabbitSettings(string Host, ushort Port, string User, string Pass);
