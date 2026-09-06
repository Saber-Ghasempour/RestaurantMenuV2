using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;

public sealed record DeleteMenuCategoryCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    long ExpectedVersion)
    : ICommand<Result<MenuCategoryId>>;
