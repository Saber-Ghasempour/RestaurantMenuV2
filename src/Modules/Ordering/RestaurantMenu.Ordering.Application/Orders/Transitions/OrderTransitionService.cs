using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Transitions;

public enum OrderTransitionAction { Accept, Reject, StartPreparing, MarkReady, MarkServed, Complete, Cancel }

public sealed class OrderTransitionService(IOrderRepository orders, IOrderStaffAccessProvider access,
    ICurrentUser currentUser, IOrderingUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<OrderTransitionResponse>> ExecuteAsync(IOrderTransitionCommand command,
        OrderTransitionAction action, CancellationToken cancellationToken)
    {
        var order = await orders.GetForUpdateAsync(command.RestaurantId, command.BranchId,
            command.OrderId, cancellationToken);
        if (order is null) return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.NotFound(command.OrderId));
        var role = await access.GetActiveRoleAsync(command.RestaurantId, command.BranchId,
            currentUser.Subject, cancellationToken);
        if (role is null || !IsAllowed(role.Value, action, order.Status))
            return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.BranchAccessRequired);
        if (order.Version != command.ExpectedVersion)
            return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.VersionConflict);
        var now = timeProvider.GetUtcNow();
        var result = action switch
        {
            OrderTransitionAction.Accept => order.Accept(currentUser.Subject, now),
            OrderTransitionAction.Reject => order.Reject(currentUser.Subject, command.Reason, now),
            OrderTransitionAction.StartPreparing => order.StartPreparing(currentUser.Subject, now),
            OrderTransitionAction.MarkReady => order.MarkReady(currentUser.Subject, now),
            OrderTransitionAction.MarkServed => order.MarkServed(currentUser.Subject, now),
            OrderTransitionAction.Complete => order.Complete(currentUser.Subject, now),
            OrderTransitionAction.Cancel => order.Cancel(currentUser.Subject, command.Reason, now),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        if (result.IsFailure) return Result.Failure<OrderTransitionResponse>(result.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException)
        { return Result.Failure<OrderTransitionResponse>(OrderTransitionErrors.VersionConflict); }
        return Result.Success(new OrderTransitionResponse(order.Id.Value, order.Status.ToString(), order.Version));
    }

    private static bool IsAllowed(OrderStaffRole role, OrderTransitionAction action, OrderStatus status) =>
        role == OrderStaffRole.Manager || (action, status, role) switch
        {
            (OrderTransitionAction.Accept or OrderTransitionAction.Reject, OrderStatus.Placed, OrderStaffRole.Cashier) => true,
            (OrderTransitionAction.StartPreparing, OrderStatus.Accepted, OrderStaffRole.Kitchen) => true,
            (OrderTransitionAction.MarkReady, OrderStatus.Preparing, OrderStaffRole.Kitchen) => true,
            (OrderTransitionAction.MarkServed, OrderStatus.Ready, OrderStaffRole.Waiter) => true,
            (OrderTransitionAction.Complete, OrderStatus.Served, OrderStaffRole.Cashier) => true,
            (OrderTransitionAction.Cancel, OrderStatus.Placed, OrderStaffRole.Cashier) => true,
            (OrderTransitionAction.Cancel, OrderStatus.Accepted or OrderStatus.Preparing, OrderStaffRole.Kitchen) => true,
            _ => false
        };
}
