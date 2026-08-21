using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sphere.Ordering.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var response = await next();
        logger.LogInformation("{Command} handled in {Ms} ms", typeof(TRequest).Name, watch.ElapsedMilliseconds);

        return response;
    }
}
