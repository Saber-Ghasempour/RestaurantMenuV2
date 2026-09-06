using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;

public sealed record UpdateMenuItemCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder,
    long ExpectedVersion)
    : ICommand<Result<long>>;
