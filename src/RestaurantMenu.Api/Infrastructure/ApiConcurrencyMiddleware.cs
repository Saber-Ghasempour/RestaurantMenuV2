using System.Net;
using System.Text.Json;

namespace RestaurantMenu.Api.Infrastructure;

public sealed class ApiConcurrencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var ifMatch = context.Request.Headers.IfMatch;
        long? requestedVersion = null;
        if (ifMatch.Count > 0)
        {
            if (ifMatch.Count != 1 || !TryParseStrongEntityTag(ifMatch[0], out var parsed))
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                    "Api.InvalidIfMatch", "If-Match must contain one strong numeric entity tag.");
                return;
            }

            requestedVersion = parsed;
            var bodyVersion = await ReadExpectedVersionAsync(context.Request);
            if (bodyVersion is not null && bodyVersion != requestedVersion)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                    "Api.InconsistentVersion", "If-Match and expectedVersion must identify the same version.");
                return;
            }
        }

        var originalBody = context.Response.Body;
        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;
        try
        {
            await next(context);

            bufferedBody.Position = 0;
            if (IsJson(context.Response.ContentType))
            {
                using var document = await TryParseJsonAsync(bufferedBody, context.RequestAborted);
                if (document is not null)
                {
                    if (TryReadVersion(document.RootElement, out var responseVersion))
                    {
                        context.Response.Headers.ETag = $"\"{responseVersion}\"";
                    }

                    if (requestedVersion is not null &&
                        context.Response.StatusCode == StatusCodes.Status409Conflict &&
                        IsConcurrencyProblem(document.RootElement))
                    {
                        context.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
                    }
                }
            }

            bufferedBody.Position = 0;
            await bufferedBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool TryParseStrongEntityTag(string? value, out long version)
    {
        version = 0;
        return value is { Length: >= 3 } && value[0] == '"' && value[^1] == '"' &&
            !value.StartsWith("W/", StringComparison.OrdinalIgnoreCase) &&
            long.TryParse(value.AsSpan(1, value.Length - 2), out version) && version > 0;
    }

    private static async Task<long?> ReadExpectedVersionAsync(HttpRequest request)
    {
        if (request.Query.TryGetValue("expectedVersion", out var queryValues) &&
            queryValues.Count == 1 && long.TryParse(queryValues[0], out var queryVersion))
            return queryVersion;
        if (request.ContentLength == 0 || !IsJson(request.ContentType)) return null;
        request.EnableBuffering();
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body,
                cancellationToken: request.HttpContext.RequestAborted);
            return document.RootElement.TryGetProperty("expectedVersion", out var value) &&
                value.TryGetInt64(out var version) ? version : null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static bool IsJson(string? contentType) =>
        contentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true ||
        contentType?.StartsWith("application/problem+json", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<JsonDocument?> TryParseJsonAsync(Stream body, CancellationToken cancellationToken)
    {
        if (body.Length == 0) return null;
        try { return await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken); }
        catch (JsonException) { return null; }
        finally { body.Position = 0; }
    }

    private static bool TryReadVersion(JsonElement root, out long version)
    {
        version = 0;
        return root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("version", out var value) && value.TryGetInt64(out version);
    }

    private static bool IsConcurrencyProblem(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("title", out var title)) return false;
        var code = title.GetString();
        return code?.Contains("VersionConflict", StringComparison.Ordinal) == true ||
            code?.Contains("Concurrency", StringComparison.Ordinal) == true;
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return Results.Problem(statusCode: status, title: title, detail: detail)
            .ExecuteAsync(context);
    }
}
