using RestaurantMenu.Application.Abstractions.Messaging;
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

    public GetRestaurantQueryHandler(
        IRestaurantReadService readService)
    {
        ArgumentNullException.ThrowIfNull(readService);

        _readService = readService;
    }

    public async Task<Result<RestaurantResponse>> Handle(
        GetRestaurantQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var restaurant =
            await _readService.GetByIdAsync(
                query.RestaurantId,
                cancellationToken);

        return restaurant is null
            ? Result.Failure<RestaurantResponse>(
                RestaurantErrors.NotFound(
                    query.RestaurantId))
            : Result.Success(restaurant);
    }
}