using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Sphere.Ordering.Application;

internal sealed class ValidationProblemHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validation)
        {
            return false;
        }

        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        await TypedResults.ValidationProblem(errors).ExecuteAsync(httpContext);

        return true;
    }
}

internal sealed class DomainProblemHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if(exception is not DomainException domain)
        {
            return false;
        }

        await TypedResults.Problem(
            title: "A business rule rejected the request.",
            detail: domain.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity).ExecuteAsync(httpContext);

        return true;
    }
}
