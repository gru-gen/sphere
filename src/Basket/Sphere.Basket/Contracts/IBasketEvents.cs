namespace Sphere.Basket.Contracts;

public interface IBasketEvents
{
    Task EnsureTopicAsync();
    Task PublishCheckedOutAsync(BasketSnapshot basketSnapshot, Guid checkoutId, CancellationToken cancellationToken);
}
