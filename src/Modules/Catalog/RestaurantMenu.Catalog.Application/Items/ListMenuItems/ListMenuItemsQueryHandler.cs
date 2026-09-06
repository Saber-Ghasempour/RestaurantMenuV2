using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.ListMenuItems;

public sealed class ListMenuItemsQueryHandler
    : IQueryHandler<
        ListMenuItemsQuery,
        Result<IReadOnlyList<MenuItemResponse>>>
{
    private readonly IMenuCategoryRepository _categoryRepository;
    private readonly IMenuItemReadService _readService;

    public ListMenuItemsQueryHandler(
        IMenuCategoryRepository categoryRepository,
        IMenuItemReadService readService)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(readService);
        _categoryRepository = categoryRepository;
        _readService = readService;
    }

    public async Task<Result<IReadOnlyList<MenuItemResponse>>> Handle(
        ListMenuItemsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var category = await _categoryRepository.GetByIdAsync(
            query.CategoryId,
            cancellationToken);

        if (category is null ||
            category.RestaurantId != query.RestaurantId)
        {
            return Result.Failure<IReadOnlyList<MenuItemResponse>>(
                MenuItemApplicationErrors.CategoryNotFound(
                    query.CategoryId.Value));
        }

        var menuItems = await _readService.GetByCategoryIdAsync(
            query.RestaurantId,
            query.CategoryId,
            cancellationToken);

        return Result.Success(menuItems);
    }
}
