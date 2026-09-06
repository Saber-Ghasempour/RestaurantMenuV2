using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed class Restaurant : AggregateRoot<RestaurantId>
{
    public const int MaxNameLength = 120;

    private Restaurant(
        RestaurantId id,
        string name,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static Result<Restaurant> Create(
        RestaurantId id,
        string? name,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameTooLong);
        }

        var restaurant = new Restaurant(
            id,
            normalizedName,
            createdAtUtc);

        restaurant.RaiseDomainEvent(
            new RestaurantCreatedDomainEvent(id));

        return Result.Success(restaurant);
    }
}