using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Items;

public sealed class MenuItemRepository : IMenuItemRepository
{
    private readonly CatalogDbContext _dbContext;

    public MenuItemRepository(CatalogDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public void Add(MenuItem menuItem)
    {
        ArgumentNullException.ThrowIfNull(menuItem);
        _dbContext.MenuItems.Add(menuItem);
    }
}
