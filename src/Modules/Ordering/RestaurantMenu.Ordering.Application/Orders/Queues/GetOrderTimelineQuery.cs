using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Queues;

public sealed record GetOrderTimelineQuery(Guid RestaurantId, Guid BranchId, OrderId OrderId)
    : IQuery<Result<IReadOnlyList<OrderTimelineEntry>>>;

public sealed class GetOrderTimelineQueryHandler(IOrderReadService reads,
    IOrderStaffAccessProvider access, ICurrentUser currentUser)
    : IQueryHandler<GetOrderTimelineQuery, Result<IReadOnlyList<OrderTimelineEntry>>>
{
    public async Task<Result<IReadOnlyList<OrderTimelineEntry>>> Handle(GetOrderTimelineQuery query,
        CancellationToken cancellationToken)
    {
        if (await access.GetActiveRoleAsync(query.RestaurantId, query.BranchId,
            currentUser.Subject, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<OrderTimelineEntry>>(OrderTransitionErrors.BranchAccessRequired);
        var timeline = await reads.GetTimelineAsync(query.RestaurantId, query.BranchId,
            query.OrderId, cancellationToken);
        return timeline is null
            ? Result.Failure<IReadOnlyList<OrderTimelineEntry>>(OrderTransitionErrors.NotFound(query.OrderId))
            : Result.Success(timeline);
    }
}
