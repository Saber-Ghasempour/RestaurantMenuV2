using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Application.Abstractions;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
}

public interface IIdempotencyRepository
{
    void Add(IdempotencyRecord record);
    Task<IdempotencyRecord?> GetAsync(string scope, string key, DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}

public sealed class IdempotencyKeyAlreadyExistsException(string message, Exception innerException)
    : Exception(message, innerException);
