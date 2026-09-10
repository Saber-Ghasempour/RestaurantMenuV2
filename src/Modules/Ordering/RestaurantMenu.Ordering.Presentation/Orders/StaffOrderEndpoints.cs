using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.Queues;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Presentation.Orders;

public static class StaffOrderEndpoints
{
    public static IEndpointRouteBuilder MapStaffOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/restaurants/{restaurantId:guid}/branches/{branchId:guid}/orders")
            .WithTags("Staff Orders");
        group.MapPost("/{orderId:guid}/accept", AcceptAsync).RequireRestaurantAccess(Permissions.OrdersAccept);
        group.MapPost("/{orderId:guid}/reject", RejectAsync).RequireRestaurantAccess(Permissions.OrdersAccept);
        group.MapPost("/{orderId:guid}/start-preparing", StartPreparingAsync).RequireRestaurantAccess(Permissions.OrdersPrepare);
        group.MapPost("/{orderId:guid}/ready", ReadyAsync).RequireRestaurantAccess(Permissions.OrdersPrepare);
        group.MapPost("/{orderId:guid}/served", ServedAsync).RequireRestaurantAccess(Permissions.OrdersServe);
        group.MapPost("/{orderId:guid}/complete", CompleteAsync).RequireRestaurantAccess(Permissions.OrdersComplete);
        group.MapPost("/{orderId:guid}/cancel", CancelAsync).RequireRestaurantAccess(Permissions.OrdersCancel);
        group.MapGet("/queues/kitchen", KitchenAsync).RequireRestaurantAccess(Permissions.OrdersRead);
        group.MapGet("/queues/cashier", CashierAsync).RequireRestaurantAccess(Permissions.OrdersRead);
        group.MapGet("/queues/waiter", WaiterAsync).RequireRestaurantAccess(Permissions.OrdersRead);
        group.MapGet("/{orderId:guid}/timeline", TimelineAsync).RequireRestaurantAccess(Permissions.OrdersRead);
        return endpoints;
    }

    private static async Task<IResult> AcceptAsync(Guid restaurantId, Guid branchId, Guid orderId,
        VersionRequest request, ICommandHandler<AcceptOrderCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion), ct));
    private static async Task<IResult> RejectAsync(Guid restaurantId, Guid branchId, Guid orderId,
        ReasonedVersionRequest request, ICommandHandler<RejectOrderCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion, request.Reason), ct));
    private static async Task<IResult> StartPreparingAsync(Guid restaurantId, Guid branchId, Guid orderId,
        VersionRequest request, ICommandHandler<StartPreparingOrderCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion), ct));
    private static async Task<IResult> ReadyAsync(Guid restaurantId, Guid branchId, Guid orderId,
        VersionRequest request, ICommandHandler<MarkOrderReadyCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion), ct));
    private static async Task<IResult> ServedAsync(Guid restaurantId, Guid branchId, Guid orderId,
        VersionRequest request, ICommandHandler<MarkOrderServedCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion), ct));
    private static async Task<IResult> CompleteAsync(Guid restaurantId, Guid branchId, Guid orderId,
        VersionRequest request, ICommandHandler<CompleteOrderCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion), ct));
    private static async Task<IResult> CancelAsync(Guid restaurantId, Guid branchId, Guid orderId,
        ReasonedVersionRequest request, ICommandHandler<CancelOrderCommand, Result<OrderTransitionResponse>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId), request.ExpectedVersion, request.Reason), ct));

    private static async Task<IResult> KitchenAsync(Guid restaurantId, Guid branchId,
        IQueryHandler<GetKitchenQueueQuery, Result<IReadOnlyList<OrderQueueItem>>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId), ct));
    private static async Task<IResult> CashierAsync(Guid restaurantId, Guid branchId,
        IQueryHandler<GetCashierQueueQuery, Result<IReadOnlyList<OrderQueueItem>>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId), ct));
    private static async Task<IResult> WaiterAsync(Guid restaurantId, Guid branchId,
        IQueryHandler<GetWaiterQueueQuery, Result<IReadOnlyList<OrderQueueItem>>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId), ct));
    private static async Task<IResult> TimelineAsync(Guid restaurantId, Guid branchId, Guid orderId,
        IQueryHandler<GetOrderTimelineQuery, Result<IReadOnlyList<OrderTimelineEntry>>> handler, CancellationToken ct) =>
        ToResult(await handler.Handle(new(restaurantId, branchId, new OrderId(orderId)), ct));

    private static IResult ToResult<T>(Result<T> result) where T : notnull =>
        result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
}

public sealed record VersionRequest(long ExpectedVersion);
public sealed record ReasonedVersionRequest(long ExpectedVersion, string? Reason);
