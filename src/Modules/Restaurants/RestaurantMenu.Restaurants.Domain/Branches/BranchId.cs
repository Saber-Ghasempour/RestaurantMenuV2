namespace RestaurantMenu.Restaurants.Domain.Branches;

public readonly record struct BranchId(Guid Value)
{
    public static BranchId New() => new(Guid.CreateVersion7());
}
