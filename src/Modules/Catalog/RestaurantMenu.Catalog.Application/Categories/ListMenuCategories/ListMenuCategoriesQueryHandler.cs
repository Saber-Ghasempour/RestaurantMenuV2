using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;

public sealed class ListMenuCategoriesQueryHandler
    : IQueryHandler<
        ListMenuCategoriesQuery,
        Result<IReadOnlyList<MenuCategoryResponse>>>
{
    private readonly IMenuCategoryReadService _readService;
    private readonly IRestaurantExistenceChecker _restaurantChecker;

    public ListMenuCategoriesQueryHandler(
        IMenuCategoryReadService readService,
        IRestaurantExistenceChecker restaurantChecker)
    {
        ArgumentNullException.ThrowIfNull(readService);
        ArgumentNullException.ThrowIfNull(restaurantChecker);
        _readService = readService;
        _restaurantChecker = restaurantChecker;
    }

    public async Task<Result<IReadOnlyList<MenuCategoryResponse>>> Handle(
        ListMenuCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!await _restaurantChecker.ExistsAsync(
                query.RestaurantId,
                cancellationToken))
        {
            return Result.Failure<IReadOnlyList<MenuCategoryResponse>>(
                MenuCategoryApplicationErrors.RestaurantNotFound(
                    query.RestaurantId));
        }

        var categories =
            await _readService.GetByRestaurantIdAsync(
                query.RestaurantId,
                cancellationToken);

        return Result.Success(categories);
    }
}
