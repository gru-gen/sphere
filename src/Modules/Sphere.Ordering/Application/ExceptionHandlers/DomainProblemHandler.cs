using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Sphere.Ordering.Application.ExceptionHandlers;

internal sealed class DomainProblemHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        // why: 422 — the request was well-formed; the business rule said no.
        await TypedResults.Problem(
            title: "A business rule rejected the request.",
            detail: domain.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity).ExecuteAsync(context);
        return true;
    }
}
