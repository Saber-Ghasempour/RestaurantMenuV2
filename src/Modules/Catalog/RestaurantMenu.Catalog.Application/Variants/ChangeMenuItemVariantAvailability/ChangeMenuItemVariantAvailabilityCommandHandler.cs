using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.ChangeMenuItemVariantAvailability;

public sealed class ChangeMenuItemVariantAvailabilityCommandHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantRepository variants,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<ChangeMenuItemVariantAvailabilityCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        ChangeMenuItemVariantAvailabilityCommand command,
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

        variant.ChangeAvailability(command.IsAvailable);
        var changed = variant.Version != command.ExpectedVersion;
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(
                MenuItemVariantApplicationErrors.VersionConflict(variant.Id));
        }

        if (changed && item.IsPublished)
        {
            await cacheInvalidator.InvalidateRestaurantAsync(item.RestaurantId, cancellationToken);
        }

        return Result.Success(variant.Version);
    }
}
