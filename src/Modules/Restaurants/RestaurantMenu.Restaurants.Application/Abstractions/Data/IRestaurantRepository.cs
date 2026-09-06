using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantRepository
{
    void Add(Restaurant restaurant);
}