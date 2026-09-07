using System.Text.Json.Serialization;

using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

namespace RestaurantMenu.Restaurants.Infrastructure.Caching;

[JsonSerializable(typeof(RestaurantResponse))]
internal sealed partial class RestaurantCacheJsonContext
    : JsonSerializerContext;
