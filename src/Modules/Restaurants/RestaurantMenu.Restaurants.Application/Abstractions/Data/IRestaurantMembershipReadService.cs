using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantMembershipReadService
{
    Task<bool> HasAccessAsync(
        RestaurantId restaurantId,
        string subject,
        CancellationToken cancellationToken);
}
