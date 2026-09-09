using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategoryContent;

public sealed record UpdateMenuCategoryContentCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    string? Description,
    long ExpectedVersion) : ICommand<Result<long>>;
