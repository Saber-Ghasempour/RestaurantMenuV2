using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.DeleteMenuItemVariant;

public sealed record DeleteMenuItemVariantCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    MenuItemVariantId VariantId,
    long ExpectedVersion) : ICommand<Result<MenuItemVariantId>>;
