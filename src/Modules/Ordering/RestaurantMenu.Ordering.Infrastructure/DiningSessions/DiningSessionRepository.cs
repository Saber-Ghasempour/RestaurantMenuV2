using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.DiningSessions;
public sealed class DiningSessionRepository(OrderingDbContext dbContext) : IDiningSessionRepository
{
    public void Add(DiningSession session) => dbContext.DiningSessions.Add(session);
    public Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.DiningSessions.AnyAsync(session => session.TokenHash == tokenHash, cancellationToken);
    public Task<DiningSessionScope?> ResolveAsync(string tokenHash, DateTimeOffset utcNow,
        CancellationToken cancellationToken) => dbContext.DiningSessions.AsNoTracking()
        .Where(session => session.TokenHash == tokenHash && session.RevokedAtUtc == null && session.ExpiresAtUtc > utcNow)
        .Select(session => new DiningSessionScope(session.Id.Value, session.RestaurantId,
            session.BranchId, session.DiningTableId, session.ExpiresAtUtc))
        .SingleOrDefaultAsync(cancellationToken);
}
