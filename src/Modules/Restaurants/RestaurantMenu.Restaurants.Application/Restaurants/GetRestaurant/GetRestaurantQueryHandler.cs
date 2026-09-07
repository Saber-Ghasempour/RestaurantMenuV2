using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

public sealed class GetRestaurantQueryHandler
    : IQueryHandler<
        GetRestaurantQuery,
        Result<RestaurantResponse>>
{
    private readonly IRestaurantReadService _readService;
    private readonly IRestaurantCache _cache;

    public GetRestaurantQueryHandler(
        IRestaurantReadService readService,
        IRestaurantCache cache)
    {
        ArgumentNullException.ThrowIfNull(readService);
        ArgumentNullException.ThrowIfNull(cache);

        _readService = readService;
        _cache = cache;
    }

    public async Task<Result<RestaurantResponse>> Handle(
        GetRestaurantQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cachedRestaurant = await _cache.GetAsync(
            query.RestaurantId,
            cancellationToken);

        if (cachedRestaurant is not null)
        {
            return Result.Success(cachedRestaurant);
        }

        var restaurant =
            await _readService.GetByIdAsync(
                query.RestaurantId,
                cancellationToken);

        if (restaurant is null)
        {
            return Result.Failure<RestaurantResponse>(
                RestaurantErrors.NotFound(
                    query.RestaurantId));
        }

        await _cache.SetAsync(
            restaurant,
            cancellationToken);

        return Result.Success(restaurant);
    }
}
