using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantMenu.Api.Infrastructure;

public sealed partial class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        LogUnhandledException(
            _logger,
            httpContext.TraceIdentifier,
            exception);

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server.UnexpectedError",
                    Detail = "An unexpected error occurred."
                }
            });
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred while processing request {TraceIdentifier}.")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string traceIdentifier,
        Exception exception);
}
