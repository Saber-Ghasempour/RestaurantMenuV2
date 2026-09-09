using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.GetMenuItemVariant;

public sealed class GetMenuItemVariantQueryHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantReadService readService)
    : IQueryHandler<GetMenuItemVariantQuery, Result<MenuItemVariantResponse>>
{
    public async Task<Result<MenuItemVariantResponse>> Handle(
        GetMenuItemVariantQuery query,
        CancellationToken cancellationToken)
    {
        var item = await menuItems.GetByIdAsync(query.MenuItemId, cancellationToken);
        if (item is null || item.RestaurantId != query.RestaurantId ||
            item.CategoryId != query.CategoryId)
        {
            return Result.Failure<MenuItemVariantResponse>(
                MenuItemVariantApplicationErrors.VariantNotFound(query.VariantId));
        }

        var response = await readService.GetByIdAsync(
            query.RestaurantId, query.MenuItemId, query.VariantId, cancellationToken);
        return response is null
            ? Result.Failure<MenuItemVariantResponse>(
                MenuItemVariantApplicationErrors.VariantNotFound(query.VariantId))
            : Result.Success(response);
    }
}
