using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;

public sealed record ChangeMenuItemAvailabilityCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    bool IsAvailable,
    long ExpectedVersion)
    : ICommand<Result<long>>;
