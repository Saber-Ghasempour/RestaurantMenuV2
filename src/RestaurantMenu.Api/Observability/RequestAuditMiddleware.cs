using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Routing;

namespace RestaurantMenu.Api.Observability;

public sealed partial class RequestAuditMiddleware(
    RequestDelegate next,
    ILogger<RequestAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startedAt = Stopwatch.GetTimestamp();
        var failed = false;
        try
        {
            await next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            Record(context, startedAt,
                failed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode);
        }
    }

    private void Record(HttpContext context, long startedAt, int statusCode)
    {
        if (!IsMutation(context.Request.Method))
            return;

        var route = context.GetEndpoint() is RouteEndpoint endpoint
            ? endpoint.RoutePattern.RawText ?? "unmatched"
            : "unmatched";
        ApiTelemetry.RecordOperation(context.Request.Method, route,
            statusCode,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        if (context.User.Identity?.IsAuthenticated != true)
            return;
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var subject = context.User.FindFirst("sub")?.Value ?? "unknown";
        var actorHash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(subject)))[..16];
        var outcome = statusCode switch
        {
            >= 200 and < 400 => "Succeeded",
            401 or 403 => "Denied",
            _ => "Failed"
        };
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(context) ?? "unavailable";
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        LogManagementMutation(
            logger,
            "ManagementMutation",
            context.Request.Method,
            route,
            outcome,
            actorHash,
            correlationId,
            traceId);
    }

    private static bool IsMutation(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        EventName = "ManagementMutationAudit",
        Message = "{AuditType} {Method} {Route} {Outcome} by {ActorHash}; correlation {CorrelationId}; trace {TraceId}.")]
    private static partial void LogManagementMutation(
        ILogger logger,
        string auditType,
        string method,
        string route,
        string outcome,
        string actorHash,
        string correlationId,
        string traceId);
}
