using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.CreateMenuItemVariant;

public sealed record CreateMenuItemVariantCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder,
    bool IsDefault) : ICommand<Result<MenuItemVariantId>>;
