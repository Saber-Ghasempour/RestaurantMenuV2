using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.ListMenuItemVariants;

public sealed class ListMenuItemVariantsQueryHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantReadService readService)
    : IQueryHandler<ListMenuItemVariantsQuery,
        Result<IReadOnlyList<MenuItemVariantResponse>>>
{
    public async Task<Result<IReadOnlyList<MenuItemVariantResponse>>> Handle(
        ListMenuItemVariantsQuery query,
        CancellationToken cancellationToken)
    {
        var item = await menuItems.GetByIdAsync(query.MenuItemId, cancellationToken);
        if (item is null || item.RestaurantId != query.RestaurantId ||
            item.CategoryId != query.CategoryId)
        {
            return Result.Failure<IReadOnlyList<MenuItemVariantResponse>>(
                MenuItemVariantApplicationErrors.MenuItemNotFound(query.MenuItemId));
        }

        return Result.Success(await readService.GetByMenuItemIdAsync(
            query.RestaurantId, query.MenuItemId, cancellationToken));
    }
}
