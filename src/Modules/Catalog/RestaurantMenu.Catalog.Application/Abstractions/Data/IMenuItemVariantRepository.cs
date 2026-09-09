using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuItemVariantRepository
{
    void Add(MenuItemVariant variant);

    Task<MenuItemVariant?> GetByIdAsync(
        MenuItemVariantId variantId,
        CancellationToken cancellationToken);

    Task<MenuItemVariant?> GetDefaultAsync(
        MenuItemId menuItemId,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(
        MenuItemId menuItemId,
        string normalizedName,
        MenuItemVariantId? excludingVariantId,
        CancellationToken cancellationToken);

    Task<bool> HasDifferentCurrencyAsync(
        MenuItemId menuItemId,
        string currency,
        MenuItemVariantId? excludingVariantId,
        CancellationToken cancellationToken);

    Task<bool> SwitchDefaultAsync(
        MenuItemVariant currentDefault,
        MenuItemVariant newDefault,
        CancellationToken cancellationToken);
}
