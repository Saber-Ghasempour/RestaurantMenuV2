using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed class IdempotencyRecord : Entity<Guid>
{
    public const int MaxScopeLength = 80;
    public const int MaxKeyLength = 128;
    public const int RequestHashLength = 64;
    private IdempotencyRecord() : base(default) { Scope = string.Empty; Key = string.Empty; RequestHash = string.Empty; }
    public IdempotencyRecord(Guid id, string scope, string key, string requestHash, OrderId resourceId,
        int responseStatus, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc) : base(id)
    {
        Scope = scope; Key = key; RequestHash = requestHash; ResourceId = resourceId;
        ResponseStatus = responseStatus; CreatedAtUtc = createdAtUtc; ExpiresAtUtc = expiresAtUtc;
    }
    public string Scope { get; }
    public string Key { get; }
    public string RequestHash { get; }
    public OrderId ResourceId { get; }
    public int ResponseStatus { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
}
