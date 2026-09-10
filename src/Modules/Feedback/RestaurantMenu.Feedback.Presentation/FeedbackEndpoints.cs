using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Application.Feedback;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Feedback.Presentation;
public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/public/orders/{orderId:guid}/feedback", CreateAsync)
            .WithTags("Feedback").AllowAnonymous().RequireRateLimiting("dining-session-use");
        var group = endpoints.MapGroup("/api/restaurants/{restaurantId:guid}/feedback").WithTags("Feedback");
        group.MapGet("/", ListAsync).RequireRestaurantAccess(Permissions.FeedbackRead);
        group.MapGet("/summary", SummaryAsync).RequireRestaurantAccess(Permissions.FeedbackRead);
        group.MapPost("/{feedbackId:guid}/hide", HideAsync).RequireRestaurantAccess(Permissions.FeedbackModerate);
        group.MapPost("/{feedbackId:guid}/restore", RestoreAsync).RequireRestaurantAccess(Permissions.FeedbackModerate);
        return endpoints;
    }
    private static async Task<IResult> CreateAsync(Guid orderId, CreateFeedbackRequest request, HttpContext context,
        ICommandHandler<CreateFeedbackCommand, Result<FeedbackItem>> handler, CancellationToken ct)
    { var token = ReadToken(context.Request); if (token is null) return Feedback.Domain.FeedbackEntries.FeedbackErrors.NotEligible.ToProblem();
      var result = await handler.Handle(new(token, orderId, request.OrderLineId, request.Rating, request.Comment), ct);
      return result.IsFailure ? result.Error.ToProblem() : Results.Created($"/api/public/orders/{orderId}/feedback", result.Value); }
    private static async Task<IResult> ListAsync(Guid restaurantId, Guid? branchId, bool? includeHidden, int? pageNumber,
        int? pageSize, IQueryHandler<ListFeedbackQuery, Result<IReadOnlyList<FeedbackItem>>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, includeHidden ?? false,
            pageNumber ?? 1, pageSize ?? 20), ct));
    private static async Task<IResult> SummaryAsync(Guid restaurantId, Guid? branchId,
        IQueryHandler<GetFeedbackSummaryQuery, Result<FeedbackSummary>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId), ct));
    private static Task<IResult> HideAsync(Guid restaurantId, Guid feedbackId, VersionRequest request,
        ICommandHandler<ModerateFeedbackCommand, Result<FeedbackItem>> h, CancellationToken ct) => Moderate(restaurantId, feedbackId, request, true, h, ct);
    private static Task<IResult> RestoreAsync(Guid restaurantId, Guid feedbackId, VersionRequest request,
        ICommandHandler<ModerateFeedbackCommand, Result<FeedbackItem>> h, CancellationToken ct) => Moderate(restaurantId, feedbackId, request, false, h, ct);
    private static async Task<IResult> Moderate(Guid rid, Guid id, VersionRequest r, bool hide,
        ICommandHandler<ModerateFeedbackCommand, Result<FeedbackItem>> h, CancellationToken ct) => ToResult(await h.Handle(new(rid,id,r.ExpectedVersion,hide),ct));
    private static IResult ToResult<T>(Result<T> result) where T:notnull => result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    private static string? ReadToken(HttpRequest request) { var hh=request.Headers.TryGetValue("X-Dining-Session",out StringValues v)&&v.Count==1&&!string.IsNullOrWhiteSpace(v[0]); var hc=request.Cookies.TryGetValue("rm_dining_session",out var c)&&!string.IsNullOrWhiteSpace(c); if(hh&&hc&&!string.Equals(v[0],c,StringComparison.Ordinal))return null; return hh?v[0]:hc?c:null; }
}
public sealed record CreateFeedbackRequest(Guid? OrderLineId, int Rating, string? Comment);
public sealed record VersionRequest(long ExpectedVersion);
