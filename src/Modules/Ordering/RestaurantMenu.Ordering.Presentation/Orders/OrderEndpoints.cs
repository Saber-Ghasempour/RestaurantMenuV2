using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Orders.PlaceOrder;
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
}

public sealed record PlaceOrderRequest(string? CustomerNote, IReadOnlyList<PlaceOrderLineRequest>? Lines);
public sealed record PlaceOrderLineRequest(Guid MenuItemId, Guid? VariantId, int Quantity, string? Note);
