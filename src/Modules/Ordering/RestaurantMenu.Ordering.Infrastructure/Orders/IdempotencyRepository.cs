using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Orders;

public sealed class IdempotencyRepository(OrderingDbContext dbContext) : IIdempotencyRepository
{
    public void Add(IdempotencyRecord record) => dbContext.IdempotencyRecords.Add(record);
    public Task<IdempotencyRecord?> GetAsync(string scope, string key, DateTimeOffset utcNow,
        CancellationToken cancellationToken) => dbContext.IdempotencyRecords.AsNoTracking()
        .SingleOrDefaultAsync(record => record.Scope == scope && record.Key == key && record.ExpiresAtUtc > utcNow, cancellationToken);
}
