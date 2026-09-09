using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.ListMenuItemVariants;

public sealed record ListMenuItemVariantsQuery(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    MenuItemId MenuItemId) : IQuery<Result<IReadOnlyList<MenuItemVariantResponse>>>;
