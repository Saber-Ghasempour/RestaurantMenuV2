using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;

public sealed record GetMenuCategoryQuery(
    Guid RestaurantId,
    MenuCategoryId CategoryId)
    : IQuery<Result<MenuCategoryResponse>>;
