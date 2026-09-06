using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.GetMenuItem;

public sealed record GetMenuItemQuery(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId)
    : IQuery<Result<MenuItemResponse>>;
