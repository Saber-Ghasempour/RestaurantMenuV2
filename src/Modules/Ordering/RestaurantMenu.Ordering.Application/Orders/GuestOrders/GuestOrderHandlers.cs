using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.GuestOrders;

public sealed record GetGuestOrderQuery(string Token, OrderId OrderId)
    : IQuery<Result<GuestOrderDetail>>;
public sealed record CancelGuestOrderCommand(string Token, OrderId OrderId,
    long ExpectedVersion, string? Reason) : ICommand<Result<OrderTransitionResponse>>;
public sealed record GuestOrderOptions(TimeSpan CancellationWindow);

public static class GuestOrderErrors
{
    public static readonly ErrorDetail CancellationWindowClosed = ErrorDetail.Conflict(
        "Ordering.GuestCancellationWindowClosed", "The guest cancellation window has closed.");
}

public sealed class GetGuestOrderQueryHandler(IDiningSessionRepository sessions,
    IDiningSessionTokenGenerator tokens, IOrderReadService reads, TimeProvider timeProvider)
    : IQueryHandler<GetGuestOrderQuery, Result<GuestOrderDetail>>
{
    public async Task<Result<GuestOrderDetail>> Handle(GetGuestOrderQuery query,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveAsync(query.Token, sessions, tokens, timeProvider, cancellationToken);
        if (scope is null) return Result.Failure<GuestOrderDetail>(DiningSessionErrors.InvalidCapability);
        var order = await reads.GetGuestOrderAsync(scope.RestaurantId, scope.BranchId,
            scope.SessionId, query.OrderId, cancellationToken);
        return order is null
            ? Result.Failure<GuestOrderDetail>(OrderTransitionErrors.NotFound(query.OrderId))
            : Result.Success(order);
    }

    internal static async Task<DiningSessionScope?> ResolveAsync(string token,
        IDiningSessionRepository sessions, IDiningSessionTokenGenerator tokens,
        TimeProvider timeProvider, CancellationToken cancellationToken) =>
        tokens.IsWellFormed(token)
            ? await sessions.ResolveAsync(tokens.Hash(token), timeProvider.GetUtcNow(), cancellationToken)
            : null;
}

public sealed class CancelGuestOrderCommandHandler(IDiningSessionRepository sessions,
    IDiningSessionTokenGenerator tokens, IOrderRepository orders, IOrderingUnitOfWork unitOfWork,
    TimeProvider timeProvider, GuestOrderOptions options)
    : ICommandHandler<CancelGuestOrderCommand, Result<OrderTransitionResponse>>
{
    public async Task<Result<OrderTransitionResponse>> Handle(CancelGuestOrderCommand command,
        CancellationToken cancellationToken)
    {
        var scope = await GetGuestOrderQueryHandler.ResolveAsync(command.Token, sessions, tokens,
            timeProvider, cancellationToken);
        if (scope is null) return Result.Failure<OrderTransitionResponse>(DiningSessionErrors.InvalidCapability);
        var order = await orders.GetForUpdateAsync(scope.RestaurantId, scope.BranchId,
            command.OrderId, cancellationToken);
        if (order is null) return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.NotFound(command.OrderId));
        if (order.DiningSessionId != scope.SessionId)
            return Result.Failure<OrderTransitionResponse>(DiningSessionErrors.InvalidCapability);
        if (order.Version != command.ExpectedVersion)
            return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.VersionConflict);
        var now = timeProvider.GetUtcNow();
        if (now > order.CreatedAtUtc.Add(options.CancellationWindow))
            return Result.Failure<OrderTransitionResponse>(GuestOrderErrors.CancellationWindowClosed);
        var result = order.CancelByGuest(command.Reason, now);
        if (result.IsFailure) return Result.Failure<OrderTransitionResponse>(result.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException)
        { return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.VersionConflict); }
        return Result.Success(new OrderTransitionResponse(order.Id.Value,
            order.Status.ToString(), order.Version));
    }
}
