using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.UpdateMenuItemMetadata;

public sealed record UpdateMenuItemMetadataCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    string? Recipe,
    int? Calories,
    IReadOnlyCollection<string>? Tags,
    string? AllergenNotes,
    int? PreparationTimeMinutes,
    bool IsFeatured,
    long ExpectedVersion) : ICommand<Result<long>>;
