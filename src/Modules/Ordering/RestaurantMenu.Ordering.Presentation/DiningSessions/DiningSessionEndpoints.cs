using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Application.DiningSessions.StartDiningSession;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Presentation.DiningSessions;
public static class DiningSessionEndpoints
{
    public const string HeaderName = "X-Dining-Session";
    public const string CookieName = "rm_dining_session";

    public static IEndpointRouteBuilder MapDiningSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/public/menu-codes/{code}/sessions", StartAsync)
            .WithTags("Dining Sessions").AllowAnonymous().RequireRateLimiting("dining-session-start");
        endpoints.MapGet("/api/public/dining-sessions/current", GetCurrentAsync)
            .WithTags("Dining Sessions").AllowAnonymous().RequireRateLimiting("dining-session-use");
        return endpoints;
    }

    private static async Task<IResult> StartAsync(string code, HttpContext context,
        ICommandHandler<StartDiningSessionCommand, Result<IssuedDiningSession>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(code), cancellationToken);
        if (result.IsFailure) return result.Error.ToProblem();
        var issued = result.Value;
        context.Response.Headers[HeaderName] = issued.Token;
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Cookies.Append(CookieName, issued.Token, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            Path = "/api/public", Expires = issued.ExpiresAtUtc, IsEssential = true
        });
        return Results.Created("/api/public/dining-sessions/current",
            new DiningSessionResponse(issued.Id, issued.RestaurantId, issued.BranchId,
                issued.DiningTableId, issued.ExpiresAtUtc));
    }

    private static async Task<IResult> GetCurrentAsync(HttpContext context,
        Guid? restaurantId, Guid? branchId, Guid? diningTableId,
        IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>> handler,
        CancellationToken cancellationToken)
    {
        var token = ReadToken(context.Request);
        if (token is null)
            return RestaurantMenu.Ordering.Domain.DiningSessions.DiningSessionErrors.InvalidCapability.ToProblem();
        var result = await handler.Handle(new(token, restaurantId, branchId, diningTableId), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    internal static string? ReadToken(HttpRequest request)
    {
        var hasHeader = request.Headers.TryGetValue(HeaderName, out StringValues values) &&
            values.Count == 1 && !string.IsNullOrWhiteSpace(values[0]);
        var hasCookie = request.Cookies.TryGetValue(CookieName, out var cookie) &&
            !string.IsNullOrWhiteSpace(cookie);
        if (hasHeader && hasCookie && !string.Equals(values[0], cookie, StringComparison.Ordinal)) return null;
        return hasHeader ? values[0] : hasCookie ? cookie : null;
    }
}

public sealed record DiningSessionResponse(Guid Id, Guid RestaurantId, Guid BranchId,
    Guid DiningTableId, DateTimeOffset ExpiresAtUtc);
