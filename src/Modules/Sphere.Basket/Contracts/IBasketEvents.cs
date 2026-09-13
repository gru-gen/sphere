namespace Sphere.Basket.Contracts;

public interface IBasketEvents
{
    Task EnsureTopicAsync();
    Task PublishCheckedOutAsync(BasketSnapshot basket, CancellationToken cancellationToken);
}
