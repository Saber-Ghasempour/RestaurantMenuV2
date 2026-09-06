using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;

public sealed record UpdateMenuCategoryCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder,
    long ExpectedVersion)
    : ICommand<Result<long>>;
