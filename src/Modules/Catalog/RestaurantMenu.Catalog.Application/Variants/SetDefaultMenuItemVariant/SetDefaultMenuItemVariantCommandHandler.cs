using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.SetDefaultMenuItemVariant;

public sealed class SetDefaultMenuItemVariantCommandHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantRepository variants,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<SetDefaultMenuItemVariantCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        SetDefaultMenuItemVariantCommand command,
        CancellationToken cancellationToken)
    {
        var item = await menuItems.GetByIdAsync(command.MenuItemId, cancellationToken);
        var variant = await variants.GetByIdAsync(command.VariantId, cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId ||
            item.CategoryId != command.CategoryId ||
            variant is null || variant.RestaurantId != command.RestaurantId ||
            variant.MenuItemId != command.MenuItemId)
        {
            return Result.Failure<long>(
                MenuItemVariantApplicationErrors.VariantNotFound(command.VariantId));
        }

        if (variant.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(
                MenuItemVariantApplicationErrors.VersionConflict(variant.Id));
        }

        var current = await variants.GetDefaultAsync(command.MenuItemId, cancellationToken);
        if (current?.Id == variant.Id)
        {
            return Result.Success(variant.Version);
        }

        if (current is not null && current.Price.Currency != variant.Price.Currency)
        {
            return Result.Failure<long>(MenuItemVariantApplicationErrors.CurrencyMismatch);
        }

        if (current is null)
        {
            return Result.Failure<long>(MenuItemVariantApplicationErrors.DefaultRequired);
        }

        if (!await variants.SwitchDefaultAsync(current, variant, cancellationToken))
        {
            return Result.Failure<long>(
                MenuItemVariantApplicationErrors.VersionConflict(variant.Id));
        }

        if (item.IsPublished)
        {
            await cacheInvalidator.InvalidateRestaurantAsync(item.RestaurantId, cancellationToken);
        }

        return Result.Success(variant.Version);
    }
}
