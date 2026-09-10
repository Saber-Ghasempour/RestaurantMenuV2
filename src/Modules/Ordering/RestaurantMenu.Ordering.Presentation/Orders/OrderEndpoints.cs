using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Orders.PlaceOrder;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.GuestOrders;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Presentation.DiningSessions;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Presentation.Orders;

public static class OrderEndpoints
{
    public const string IdempotencyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/public/orders", PlaceAsync)
            .WithTags("Orders").AllowAnonymous().RequireRateLimiting("dining-session-use")
            .Produces<PlaceOrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem().ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
        endpoints.MapGet("/api/public/orders/{orderId:guid}", GetAsync)
            .WithTags("Orders").AllowAnonymous().RequireRateLimiting("dining-session-use");
        endpoints.MapPost("/api/public/orders/{orderId:guid}/cancel", CancelAsync)
            .WithTags("Orders").AllowAnonymous().RequireRateLimiting("dining-session-use");
        return endpoints;
    }

    private static async Task<IResult> PlaceAsync(PlaceOrderRequest request, HttpContext context,
        ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var token = DiningSessionEndpoints.ReadToken(context.Request);
        if (token is null) return DiningSessionErrors.InvalidCapability.ToProblem();
        var key = context.Request.Headers.TryGetValue(IdempotencyHeaderName, out StringValues values) && values.Count == 1
            ? values[0] ?? string.Empty : string.Empty;
        var result = await handler.Handle(new PlaceOrderCommand(token, key, request.CustomerNote,
            request.Lines?.Select(line => new PlaceOrderLine(line.MenuItemId, line.VariantId,
                line.Quantity, line.Note)).ToArray()), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem()
            : Results.Created($"/api/public/orders/{result.Value.OrderId}", result.Value);
    }

    private static async Task<IResult> GetAsync(Guid orderId, HttpContext context,
        IQueryHandler<GetGuestOrderQuery, Result<GuestOrderDetail>> handler,
        CancellationToken cancellationToken)
    {
        var token = DiningSessionEndpoints.ReadToken(context.Request);
        if (token is null) return DiningSessionErrors.InvalidCapability.ToProblem();
        var result = await handler.Handle(new GetGuestOrderQuery(token, new OrderId(orderId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> CancelAsync(Guid orderId, CancelGuestOrderRequest request,
        HttpContext context,
        ICommandHandler<CancelGuestOrderCommand, Result<OrderTransitionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var token = DiningSessionEndpoints.ReadToken(context.Request);
        if (token is null) return DiningSessionErrors.InvalidCapability.ToProblem();
        var result = await handler.Handle(new CancelGuestOrderCommand(token, new OrderId(orderId),
            request.ExpectedVersion, request.Reason), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
}

public sealed record PlaceOrderRequest(string? CustomerNote, IReadOnlyList<PlaceOrderLineRequest>? Lines);
public sealed record PlaceOrderLineRequest(Guid MenuItemId, Guid? VariantId, int Quantity, string? Note);
public sealed record CancelGuestOrderRequest(long ExpectedVersion, string? Reason);
