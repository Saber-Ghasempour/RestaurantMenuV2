using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.GetMenuItem;

public sealed class GetMenuItemQueryHandler
    : IQueryHandler<GetMenuItemQuery, Result<MenuItemResponse>>
{
    private readonly IMenuItemReadService _readService;

    public GetMenuItemQueryHandler(IMenuItemReadService readService)
    {
        ArgumentNullException.ThrowIfNull(readService);
        _readService = readService;
    }

    public async Task<Result<MenuItemResponse>> Handle(
        GetMenuItemQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var menuItem = await _readService.GetByIdAsync(
            query.RestaurantId,
            query.CategoryId,
            query.MenuItemId,
            cancellationToken);

        return menuItem is null
            ? Result.Failure<MenuItemResponse>(
                MenuItemApplicationErrors.ItemNotFound(
                    query.MenuItemId))
            : Result.Success(menuItem);
    }
}
