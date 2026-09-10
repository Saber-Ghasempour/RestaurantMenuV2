using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Transitions;

public interface IOrderTransitionCommand
{
    Guid RestaurantId { get; }
    Guid BranchId { get; }
    OrderId OrderId { get; }
    long ExpectedVersion { get; }
    string? Reason { get; }
}

public sealed record AcceptOrderCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand
{ public string? Reason => null; }
public sealed record RejectOrderCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion, string? Reason) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand;
public sealed record StartPreparingOrderCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand
{ public string? Reason => null; }
public sealed record MarkOrderReadyCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand
{ public string? Reason => null; }
public sealed record MarkOrderServedCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand
{ public string? Reason => null; }
public sealed record CompleteOrderCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand
{ public string? Reason => null; }
public sealed record CancelOrderCommand(Guid RestaurantId, Guid BranchId, OrderId OrderId,
    long ExpectedVersion, string? Reason) : ICommand<Result<OrderTransitionResponse>>, IOrderTransitionCommand;

public sealed record OrderTransitionResponse(Guid Id, string Status, long Version);
