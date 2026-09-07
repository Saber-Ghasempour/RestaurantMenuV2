using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;

public sealed record ListRestaurantsQuery(
    string Subject,
    int Page = ListRestaurantsQuery.DefaultPage,
    int PageSize = ListRestaurantsQuery.DefaultPageSize)
    : IQuery<Result<RestaurantsPage>>
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
