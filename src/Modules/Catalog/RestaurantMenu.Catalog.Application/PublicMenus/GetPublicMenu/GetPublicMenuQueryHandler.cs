using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

public sealed class GetPublicMenuQueryHandler
    : IQueryHandler<GetPublicMenuQuery, Result<PublicMenuResponse>>
{
    private readonly IPublicMenuReadService _publicMenuReadService;
    private readonly IRestaurantPublicProfileProvider _restaurantProvider;

    public GetPublicMenuQueryHandler(
        IPublicMenuReadService publicMenuReadService,
        IRestaurantPublicProfileProvider restaurantProvider)
    {
        ArgumentNullException.ThrowIfNull(publicMenuReadService);
        ArgumentNullException.ThrowIfNull(restaurantProvider);
        _publicMenuReadService = publicMenuReadService;
        _restaurantProvider = restaurantProvider;
    }

    public async Task<Result<PublicMenuResponse>> Handle(
        GetPublicMenuQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var restaurant = await _restaurantProvider.GetAsync(
            query.RestaurantId,
            cancellationToken);

        if (restaurant is null)
        {
            return Result.Failure<PublicMenuResponse>(
                CatalogApplicationErrors.RestaurantNotFound(
                    query.RestaurantId));
        }

        var categories =
            await _publicMenuReadService.GetByRestaurantIdAsync(
                query.RestaurantId,
                cancellationToken);

        return Result.Success(
            new PublicMenuResponse(
                restaurant.Id,
                restaurant.Name,
                categories,
                restaurant.Description,
                restaurant.About,
                restaurant.Address));
    }
}
