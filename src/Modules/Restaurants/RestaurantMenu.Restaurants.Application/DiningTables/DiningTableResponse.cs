namespace RestaurantMenu.Restaurants.Application.DiningTables;

public sealed record DiningTableResponse(Guid Id, Guid RestaurantId, Guid BranchId,
    int Number, string? DisplayName, short? Capacity, bool IsActive,
    DateTimeOffset CreatedAtUtc, long Version);
