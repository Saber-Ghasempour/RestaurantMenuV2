using RestaurantMenu.Ordering.Domain.DiningSessions;

namespace RestaurantMenu.Ordering.Application.Abstractions;

public sealed record DiningSessionScope(Guid SessionId, Guid RestaurantId,
    Guid BranchId, Guid DiningTableId, DateTimeOffset ExpiresAtUtc);

public interface IDiningSessionRepository
{
    void Add(DiningSession session);
    Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken);
    Task<DiningSessionScope?> ResolveAsync(string tokenHash, DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}

public interface IOrderingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class DiningSessionTokenHashAlreadyExistsException(string message, Exception innerException)
    : Exception(message, innerException);
