using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Queues;

public sealed class OrderQueueQueryService(IOrderReadService reads, IOrderStaffAccessProvider access,
    ICurrentUser currentUser)
{
    public async Task<Result<IReadOnlyList<OrderQueueItem>>> GetAsync(Guid restaurantId, Guid branchId,
        OrderStaffRole queueRole, IReadOnlyCollection<OrderStatus> statuses, CancellationToken cancellationToken)
    {
        var role = await access.GetActiveRoleAsync(restaurantId, branchId, currentUser.Subject, cancellationToken);
        if (role is null || role != OrderStaffRole.Manager && role != queueRole)
            return Result.Failure<IReadOnlyList<OrderQueueItem>>(OrderTransitionErrors.BranchAccessRequired);
        return Result.Success(await reads.ListQueueAsync(restaurantId, branchId, statuses, cancellationToken));
    }
}

public sealed class GetKitchenQueueQueryHandler(OrderQueueQueryService service) : IQueryHandler<GetKitchenQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>
{ public Task<Result<IReadOnlyList<OrderQueueItem>>> Handle(GetKitchenQueueQuery query, CancellationToken cancellationToken) => service.GetAsync(query.RestaurantId, query.BranchId, OrderStaffRole.Kitchen, [OrderStatus.Accepted, OrderStatus.Preparing], cancellationToken); }
public sealed class GetCashierQueueQueryHandler(OrderQueueQueryService service) : IQueryHandler<GetCashierQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>
{ public Task<Result<IReadOnlyList<OrderQueueItem>>> Handle(GetCashierQueueQuery query, CancellationToken cancellationToken) => service.GetAsync(query.RestaurantId, query.BranchId, OrderStaffRole.Cashier, [OrderStatus.Placed, OrderStatus.Served], cancellationToken); }
public sealed class GetWaiterQueueQueryHandler(OrderQueueQueryService service) : IQueryHandler<GetWaiterQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>
{ public Task<Result<IReadOnlyList<OrderQueueItem>>> Handle(GetWaiterQueueQuery query, CancellationToken cancellationToken) => service.GetAsync(query.RestaurantId, query.BranchId, OrderStaffRole.Waiter, [OrderStatus.Ready], cancellationToken); }
