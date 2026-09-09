namespace RestaurantMenu.Ordering.Domain.DiningSessions;
public readonly record struct DiningSessionId(Guid Value)
{
    public static DiningSessionId New() => new(Guid.CreateVersion7());
}
