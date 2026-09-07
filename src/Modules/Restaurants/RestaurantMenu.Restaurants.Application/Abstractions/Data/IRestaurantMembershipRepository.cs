using RestaurantMenu.Restaurants.Domain.Memberships;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantMembershipRepository
{
    void Add(RestaurantMembership membership);
}
