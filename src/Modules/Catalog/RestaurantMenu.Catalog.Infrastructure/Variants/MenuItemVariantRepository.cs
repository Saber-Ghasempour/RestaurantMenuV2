using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Variants;

public sealed class MenuItemVariantRepository(CatalogDbContext dbContext)
    : IMenuItemVariantRepository
{
    public void Add(MenuItemVariant variant) => dbContext.MenuItemVariants.Add(variant);

    public Task<MenuItemVariant?> GetByIdAsync(
        MenuItemVariantId variantId,
        CancellationToken cancellationToken) =>
        dbContext.MenuItemVariants.SingleOrDefaultAsync(
            variant => variant.Id == variantId, cancellationToken);

    public Task<MenuItemVariant?> GetDefaultAsync(
        MenuItemId menuItemId,
        CancellationToken cancellationToken) =>
        dbContext.MenuItemVariants.SingleOrDefaultAsync(
            variant => variant.MenuItemId == menuItemId && variant.IsDefault,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        MenuItemId menuItemId,
        string normalizedName,
        MenuItemVariantId? excludingVariantId,
        CancellationToken cancellationToken) =>
        dbContext.MenuItemVariants.AnyAsync(
            variant => variant.MenuItemId == menuItemId &&
                variant.Name == normalizedName &&
                (!excludingVariantId.HasValue || variant.Id != excludingVariantId.Value),
            cancellationToken);

    public Task<bool> HasDifferentCurrencyAsync(
        MenuItemId menuItemId,
        string currency,
        MenuItemVariantId? excludingVariantId,
        CancellationToken cancellationToken) =>
        dbContext.MenuItemVariants.AnyAsync(
            variant => variant.MenuItemId == menuItemId &&
                variant.Price.Currency != currency &&
                (!excludingVariantId.HasValue || variant.Id != excludingVariantId.Value),
            cancellationToken);

    public async Task<bool> SwitchDefaultAsync(
        MenuItemVariant currentDefault,
        MenuItemVariant newDefault,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        var cleared = await dbContext.MenuItemVariants
            .Where(variant => variant.Id == currentDefault.Id &&
                variant.Version == currentDefault.Version && variant.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(variant => variant.IsDefault, false)
                .SetProperty(variant => variant.Version,
                    variant => variant.Version + 1), cancellationToken);
        if (cleared != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var selected = await dbContext.MenuItemVariants
            .Where(variant => variant.Id == newDefault.Id &&
                variant.Version == newDefault.Version && !variant.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(variant => variant.IsDefault, true)
                .SetProperty(variant => variant.Version,
                    variant => variant.Version + 1), cancellationToken);
        if (selected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        currentDefault.RemoveDefault();
        newDefault.MakeDefault();
        return true;
    }
}
