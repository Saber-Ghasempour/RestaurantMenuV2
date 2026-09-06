using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;

public sealed record ListMenuCategoriesQuery(Guid RestaurantId)
    : IQuery<Result<IReadOnlyList<MenuCategoryResponse>>>;
