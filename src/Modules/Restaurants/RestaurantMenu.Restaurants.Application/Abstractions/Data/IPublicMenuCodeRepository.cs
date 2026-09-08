using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IPublicMenuCodeRepository
{
    void Add(PublicMenuCode code);
    Task<PublicMenuCode?> GetByIdAsync(RestaurantId restaurantId, PublicMenuCodeId id,
        CancellationToken cancellationToken);
    Task<bool> CodeHashExistsAsync(string codeHash, CancellationToken cancellationToken);
}

public sealed class PublicMenuCodeHashAlreadyExistsException(string message, Exception innerException)
    : Exception(message, innerException);
