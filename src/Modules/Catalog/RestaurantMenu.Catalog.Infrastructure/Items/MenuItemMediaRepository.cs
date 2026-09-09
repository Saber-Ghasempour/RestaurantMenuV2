using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Items;

public sealed class MenuItemMediaRepository(CatalogDbContext dbContext) : IMenuItemMediaRepository
{
    public async Task ReplaceAsync(Guid restaurantId, MenuItemId menuItemId, IReadOnlyCollection<MenuItemMedia> media, CancellationToken cancellationToken)
    {
        var existing = await dbContext.MenuItemMedia.Where(x => x.RestaurantId == restaurantId && x.MenuItemId == menuItemId).ToArrayAsync(cancellationToken);
        dbContext.MenuItemMedia.RemoveRange(existing);
        dbContext.MenuItemMedia.AddRange(media);
    }
}
