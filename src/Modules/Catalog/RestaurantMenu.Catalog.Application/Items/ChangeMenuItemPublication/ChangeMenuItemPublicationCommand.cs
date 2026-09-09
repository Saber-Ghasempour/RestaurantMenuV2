using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.ChangeMenuItemPublication;

public sealed record ChangeMenuItemPublicationCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId,
    bool IsPublished,
    long ExpectedVersion) : ICommand<Result<long>>;
