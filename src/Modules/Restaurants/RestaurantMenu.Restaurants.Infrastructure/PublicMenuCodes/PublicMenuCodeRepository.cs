using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.PublicMenuCodes;
public sealed class PublicMenuCodeRepository(RestaurantsDbContext dbContext) : IPublicMenuCodeRepository
{
    public void Add(PublicMenuCode code) => dbContext.PublicMenuCodes.Add(code);
    public Task<PublicMenuCode?> GetByIdAsync(RestaurantId restaurantId, PublicMenuCodeId id,
        CancellationToken cancellationToken) => dbContext.PublicMenuCodes.SingleOrDefaultAsync(
            code => code.RestaurantId == restaurantId && code.Id == id, cancellationToken);
    public Task<bool> CodeHashExistsAsync(string codeHash, CancellationToken cancellationToken) =>
        dbContext.PublicMenuCodes.AnyAsync(code => code.CodeHash == codeHash, cancellationToken);
}
