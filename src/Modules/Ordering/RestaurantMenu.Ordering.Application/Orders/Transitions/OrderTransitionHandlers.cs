using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Transitions;

public sealed class AcceptOrderCommandHandler(OrderTransitionService service) : ICommandHandler<AcceptOrderCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(AcceptOrderCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.Accept, cancellationToken); }
public sealed class RejectOrderCommandHandler(OrderTransitionService service) : ICommandHandler<RejectOrderCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(RejectOrderCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.Reject, cancellationToken); }
public sealed class StartPreparingOrderCommandHandler(OrderTransitionService service) : ICommandHandler<StartPreparingOrderCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(StartPreparingOrderCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.StartPreparing, cancellationToken); }
public sealed class MarkOrderReadyCommandHandler(OrderTransitionService service) : ICommandHandler<MarkOrderReadyCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(MarkOrderReadyCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.MarkReady, cancellationToken); }
public sealed class MarkOrderServedCommandHandler(OrderTransitionService service) : ICommandHandler<MarkOrderServedCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(MarkOrderServedCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.MarkServed, cancellationToken); }
public sealed class CompleteOrderCommandHandler(OrderTransitionService service) : ICommandHandler<CompleteOrderCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(CompleteOrderCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.Complete, cancellationToken); }
public sealed class CancelOrderCommandHandler(OrderTransitionService service) : ICommandHandler<CancelOrderCommand, Result<OrderTransitionResponse>>
{ public Task<Result<OrderTransitionResponse>> Handle(CancelOrderCommand command, CancellationToken cancellationToken) => service.ExecuteAsync(command, OrderTransitionAction.Cancel, cancellationToken); }
