using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.UpdateMenuItemVariant;

public sealed record UpdateMenuItemVariantCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    MenuItemVariantId VariantId,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder,
    long ExpectedVersion) : ICommand<Result<long>>;
