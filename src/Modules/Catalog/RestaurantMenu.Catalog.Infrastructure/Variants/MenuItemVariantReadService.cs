using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Variants;

public sealed class MenuItemVariantReadService(CatalogDbContext dbContext)
    : IMenuItemVariantReadService
{
    public Task<MenuItemVariantResponse?> GetByIdAsync(
        Guid restaurantId, MenuItemId menuItemId, MenuItemVariantId variantId,
        CancellationToken cancellationToken) =>
        Project(Query(restaurantId, menuItemId)
                .Where(variant => variant.Id == variantId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MenuItemVariantResponse>> GetByMenuItemIdAsync(
        Guid restaurantId, MenuItemId menuItemId,
        CancellationToken cancellationToken) =>
        await Project(Query(restaurantId, menuItemId)
                .OrderByDescending(variant => variant.IsDefault)
                .ThenBy(variant => variant.DisplayOrder)
                .ThenBy(variant => variant.Name)
                .ThenBy(variant => variant.Id))
            .ToArrayAsync(cancellationToken);

    private IQueryable<MenuItemVariant> Query(
        Guid restaurantId, MenuItemId menuItemId) =>
        dbContext.MenuItemVariants.AsNoTracking()
            .Where(variant => variant.RestaurantId == restaurantId &&
                variant.MenuItemId == menuItemId);

    private static IQueryable<MenuItemVariantResponse> Project(
        IQueryable<MenuItemVariant> query) =>
        query.Select(variant => new MenuItemVariantResponse(
                variant.Id.Value, variant.RestaurantId, variant.MenuItemId.Value,
                variant.Name, variant.Description, variant.Price.Amount,
                variant.Price.Currency, variant.DisplayOrder, variant.IsDefault,
                variant.IsAvailable, variant.CreatedAtUtc, variant.Version));
}
