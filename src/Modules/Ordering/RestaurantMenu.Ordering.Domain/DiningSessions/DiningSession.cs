using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Domain.DiningSessions;

public sealed class DiningSession : AggregateRoot<DiningSessionId>
{
    public const int MaxTokenHashLength = 128;

    private DiningSession(DiningSessionId id, string tokenHash, Guid restaurantId,
        Guid branchId, Guid diningTableId, DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc) : base(id)
    {
        TokenHash = tokenHash; RestaurantId = restaurantId; BranchId = branchId;
        DiningTableId = diningTableId; CreatedAtUtc = createdAtUtc; ExpiresAtUtc = expiresAtUtc;
    }

    private DiningSession(DiningSessionId id, string tokenHash, Guid restaurantId,
        Guid branchId, Guid diningTableId, DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc, DateTimeOffset? revokedAtUtc,
        DateTimeOffset? lastSeenAtUtc) : this(id, tokenHash, restaurantId, branchId,
            diningTableId, createdAtUtc, expiresAtUtc)
    {
        RevokedAtUtc = revokedAtUtc; LastSeenAtUtc = lastSeenAtUtc;
    }

    public string TokenHash { get; }
    public Guid RestaurantId { get; }
    public Guid BranchId { get; }
    public Guid DiningTableId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    public static Result<DiningSession> Create(DiningSessionId id, string? tokenHash,
        Guid restaurantId, Guid branchId, Guid diningTableId,
        DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > MaxTokenHashLength)
            return Result.Failure<DiningSession>(DiningSessionErrors.InvalidTokenHash);
        if (restaurantId == Guid.Empty || branchId == Guid.Empty || diningTableId == Guid.Empty)
            return Result.Failure<DiningSession>(DiningSessionErrors.InvalidScope);
        if (expiresAtUtc <= createdAtUtc)
            return Result.Failure<DiningSession>(DiningSessionErrors.InvalidExpiry);
        return Result.Success(new DiningSession(id, tokenHash, restaurantId, branchId,
            diningTableId, createdAtUtc, expiresAtUtc));
    }

    public bool IsActiveAt(DateTimeOffset utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is null) RevokedAtUtc = revokedAtUtc;
    }

    public void MarkSeen(DateTimeOffset seenAtUtc)
    {
        if (IsActiveAt(seenAtUtc)) LastSeenAtUtc = seenAtUtc;
    }
}
