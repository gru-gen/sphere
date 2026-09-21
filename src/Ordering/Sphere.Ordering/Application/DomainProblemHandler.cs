using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Sphere.Ordering.Application;

internal sealed class DomainProblemHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        await TypedResults.Problem(
            title: "A business rule rejected the request.",
            detail: domainException.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity).ExecuteAsync(httpContext);
        return true;
    }
}
