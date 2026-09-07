using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;

public sealed class ListRestaurantsQueryHandler
    : IQueryHandler<
        ListRestaurantsQuery,
        Result<RestaurantsPage>>
{
    private readonly IRestaurantReadService _readService;

    public ListRestaurantsQueryHandler(
        IRestaurantReadService readService)
    {
        ArgumentNullException.ThrowIfNull(readService);

        _readService = readService;
    }

    public async Task<Result<RestaurantsPage>> Handle(
        ListRestaurantsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1)
        {
            return Result.Failure<RestaurantsPage>(
                ListRestaurantsErrors.InvalidPage);
        }

        if (query.PageSize is < 1
            or > ListRestaurantsQuery.MaxPageSize)
        {
            return Result.Failure<RestaurantsPage>(
                ListRestaurantsErrors.InvalidPageSize);
        }

        var page =
            await _readService.GetPageAsync(
                query.Subject,
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result.Success(page);
    }
}
