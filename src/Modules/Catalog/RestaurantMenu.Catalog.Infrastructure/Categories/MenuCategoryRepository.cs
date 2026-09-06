using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Categories;

public sealed class MenuCategoryRepository
    : IMenuCategoryRepository
{
    private readonly CatalogDbContext _dbContext;

    public MenuCategoryRepository(CatalogDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public void Add(MenuCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        _dbContext.MenuCategories.Add(category);
    }

    public Task<MenuCategory?> GetByIdAsync(
        MenuCategoryId categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.MenuCategories.SingleOrDefaultAsync(
            category => category.Id == categoryId,
            cancellationToken);
    }
}
