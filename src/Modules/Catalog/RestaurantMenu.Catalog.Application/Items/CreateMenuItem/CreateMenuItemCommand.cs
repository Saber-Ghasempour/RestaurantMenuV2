using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.Items.CreateMenuItem;

public sealed record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid CategoryId,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder)
    : ICommand<Result<MenuItemId>>;
