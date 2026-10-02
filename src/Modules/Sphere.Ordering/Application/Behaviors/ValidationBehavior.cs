using FluentValidation;

namespace Sphere.Ordering.Application.Behaviors;

// summary: every command is validated before its handler runs, no matter who sent it.
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                // why: throw here, translate once at the edge — handlers stay clean.
                throw new ValidationException(result.Errors);
            }
        }

        return await next();
    }
}
