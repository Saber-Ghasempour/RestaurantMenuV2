namespace RestaurantMenu.Restaurants.Domain.PublicMenuCodes;

public readonly record struct PublicMenuCodeId(Guid Value)
{
    public static PublicMenuCodeId New() => new(Guid.NewGuid());
}
