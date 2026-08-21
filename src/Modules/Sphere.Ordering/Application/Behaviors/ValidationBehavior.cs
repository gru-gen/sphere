using FluentValidation;

namespace Sphere.Ordering.Application.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        return await next();
    }
}
