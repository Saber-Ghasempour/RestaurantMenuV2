using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.ListMenuItems;

public sealed record ListMenuItemsQuery(
    Guid RestaurantId,
    MenuCategoryId CategoryId)
    : IQuery<Result<IReadOnlyList<MenuItemResponse>>>;
