using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;

public sealed record CreateMenuCategoryCommand(
    Guid RestaurantId,
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder)
    : ICommand<Result<MenuCategoryId>>;
