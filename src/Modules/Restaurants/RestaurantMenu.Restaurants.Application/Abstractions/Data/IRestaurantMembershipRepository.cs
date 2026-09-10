using RestaurantMenu.Restaurants.Domain.Memberships;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantMembershipRepository
{
    void Add(RestaurantMembership membership);
    Task<RestaurantMembership?> GetAsync(RestaurantMenu.Restaurants.Domain.Restaurants.RestaurantId restaurantId, string subject, CancellationToken cancellationToken) => Task.FromResult<RestaurantMembership?>(null);
    Task<int> CountActiveOwnersAsync(RestaurantMenu.Restaurants.Domain.Restaurants.RestaurantId restaurantId, CancellationToken cancellationToken) => Task.FromResult(0);
    Task<IReadOnlyList<RestaurantMenu.Restaurants.Application.Memberships.MemberResponse>> ListAsync(RestaurantMenu.Restaurants.Domain.Restaurants.RestaurantId restaurantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RestaurantMenu.Restaurants.Application.Memberships.MemberResponse>>([]);
}