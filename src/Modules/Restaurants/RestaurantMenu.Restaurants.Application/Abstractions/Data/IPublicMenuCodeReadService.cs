using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IPublicMenuCodeReadService
{
    Task<IReadOnlyList<PublicMenuCodeResponse>> ListAsync(RestaurantId restaurantId,
        CancellationToken cancellationToken);
    Task<ResolvedPublicMenuCode?> ResolveAsync(string codeHash, DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
