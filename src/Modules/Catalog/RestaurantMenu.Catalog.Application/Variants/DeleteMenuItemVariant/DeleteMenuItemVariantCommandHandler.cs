using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.DeleteMenuItemVariant;

public sealed class DeleteMenuItemVariantCommandHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantRepository variants,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator,
    TimeProvider timeProvider)
    : ICommandHandler<DeleteMenuItemVariantCommand, Result<MenuItemVariantId>>
{
    public async Task<Result<MenuItemVariantId>> Handle(
        DeleteMenuItemVariantCommand command,
        CancellationToken cancellationToken)
    {
        var item = await menuItems.GetByIdAsync(command.MenuItemId, cancellationToken);
        var variant = await variants.GetByIdAsync(command.VariantId, cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId ||
            item.CategoryId != command.CategoryId ||
            variant is null || variant.RestaurantId != command.RestaurantId ||
            variant.MenuItemId != command.MenuItemId)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.VariantNotFound(command.VariantId));
        }

        if (variant.Version != command.ExpectedVersion)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.VersionConflict(variant.Id));
        }

        var result = variant.Delete(timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return Result.Failure<MenuItemVariantId>(result.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.VersionConflict(variant.Id));
        }

        if (item.IsPublished)
        {
            await cacheInvalidator.InvalidateRestaurantAsync(item.RestaurantId, cancellationToken);
        }

        return Result.Success(variant.Id);
    }
}
