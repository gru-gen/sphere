namespace Sphere.Notification.Service.Data;

internal sealed class SentNotification
{
    public Guid Id { get; init; }
    public Guid OrderId {  get; init; }
    public Guid CustomerId {  get; init; }
    public required string Channel { get; init; }
    public string? Subject { get; init; }
    public required string Body { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
