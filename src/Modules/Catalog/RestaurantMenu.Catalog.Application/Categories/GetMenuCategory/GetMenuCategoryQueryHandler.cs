using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;

public sealed class GetMenuCategoryQueryHandler
    : IQueryHandler<
        GetMenuCategoryQuery,
        Result<MenuCategoryResponse>>
{
    private readonly IMenuCategoryReadService _readService;

    public GetMenuCategoryQueryHandler(
        IMenuCategoryReadService readService)
    {
        ArgumentNullException.ThrowIfNull(readService);
        _readService = readService;
    }

    public async Task<Result<MenuCategoryResponse>> Handle(
        GetMenuCategoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var category = await _readService.GetByIdAsync(
            query.RestaurantId,
            query.CategoryId,
            cancellationToken);

        return category is null
            ? Result.Failure<MenuCategoryResponse>(
                MenuCategoryApplicationErrors.CategoryNotFound(
                    query.CategoryId))
            : Result.Success(category);
    }
}
