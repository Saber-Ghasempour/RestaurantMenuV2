namespace RestaurantMenu.Ordering.Application.DiningSessions;
public sealed record IssuedDiningSession(Guid Id, string Token, Guid RestaurantId,
    Guid BranchId, Guid DiningTableId, DateTimeOffset ExpiresAtUtc);
