using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Api.Integrations.Ordering;
public sealed class FeedbackEligibilityProvider(
    IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>> sessions,
    IOrderFeedbackReadService orders) : IFeedbackEligibilityProvider
{
    public async Task<FeedbackEligibility?> GetAsync(string token, Guid orderId, CancellationToken cancellationToken)
    {
        var session = await sessions.Handle(new(token), cancellationToken);
        if (session.IsFailure) return null;
        var s = session.Value;
        var order = await orders.GetFeedbackEligibilityAsync(s.RestaurantId, s.BranchId, s.SessionId, new(orderId), cancellationToken);
        return order is null ? null : new(s.RestaurantId, s.BranchId, order.OrderId, s.SessionId, order.CompletedAtUtc, order.OrderLineIds);
    }
}
