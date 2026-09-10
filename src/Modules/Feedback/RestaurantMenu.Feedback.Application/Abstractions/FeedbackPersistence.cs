using RestaurantMenu.Feedback.Domain.FeedbackEntries;
namespace RestaurantMenu.Feedback.Application.Abstractions;
public sealed record FeedbackEligibility(Guid RestaurantId, Guid BranchId, Guid OrderId,
    Guid DiningSessionId, DateTimeOffset CompletedAtUtc, IReadOnlySet<Guid> OrderLineIds);
public interface IFeedbackEligibilityProvider { Task<FeedbackEligibility?> GetAsync(string token, Guid orderId, CancellationToken cancellationToken); }
public interface IFeedbackEntryRepository
{
    void Add(FeedbackEntry entry);
    Task<FeedbackEntry?> GetAsync(Guid restaurantId, FeedbackEntryId id, CancellationToken cancellationToken);
}
public interface IFeedbackUnitOfWork { Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); }
public sealed class DuplicateFeedbackException(string message, Exception inner) : Exception(message, inner);
public sealed class FeedbackConcurrencyException(string message, Exception inner) : Exception(message, inner);
