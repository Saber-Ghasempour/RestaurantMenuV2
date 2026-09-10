using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Queues;

public sealed record GetKitchenQueueQuery(Guid RestaurantId, Guid BranchId) : IQuery<Result<IReadOnlyList<OrderQueueItem>>>;
public sealed record GetCashierQueueQuery(Guid RestaurantId, Guid BranchId) : IQuery<Result<IReadOnlyList<OrderQueueItem>>>;
public sealed record GetWaiterQueueQuery(Guid RestaurantId, Guid BranchId) : IQuery<Result<IReadOnlyList<OrderQueueItem>>>;
