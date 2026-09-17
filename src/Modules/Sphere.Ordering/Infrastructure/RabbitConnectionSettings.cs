namespace Sphere.Ordering.Infrastructure;

internal sealed record RabbitConnectionSettings(string Host, ushort Port, string User, string Password);
