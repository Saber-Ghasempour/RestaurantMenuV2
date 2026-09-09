using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.ChangeMenuItemVariantAvailability;

public sealed record ChangeMenuItemVariantAvailabilityCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    MenuItemVariantId VariantId,
    bool IsAvailable,
    long ExpectedVersion) : ICommand<Result<long>>;
