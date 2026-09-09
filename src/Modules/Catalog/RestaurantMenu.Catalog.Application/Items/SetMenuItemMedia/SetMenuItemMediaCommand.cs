using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.SetMenuItemMedia;

public sealed record MenuItemMediaInput(Guid MediaAssetId, int DisplayOrder, string? AltText, bool IsPrimary);
public sealed record SetMenuItemMediaCommand(Guid RestaurantId, MenuCategoryId CategoryId, MenuItemId MenuItemId,
    IReadOnlyList<MenuItemMediaInput> Media, long ExpectedVersion) : ICommand<Result<long>>;
