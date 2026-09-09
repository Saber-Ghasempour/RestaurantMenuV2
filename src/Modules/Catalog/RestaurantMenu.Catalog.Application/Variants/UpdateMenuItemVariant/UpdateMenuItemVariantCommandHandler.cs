using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.UpdateMenuItemVariant;

public sealed class UpdateMenuItemVariantCommandHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantRepository variants,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<UpdateMenuItemVariantCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        UpdateMenuItemVariantCommand command,
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

        var candidate = MenuItemVariant.Create(
            variant.Id, variant.RestaurantId, variant.MenuItemId,
            command.Name, command.Description, command.PriceAmount,
            command.Currency, command.DisplayOrder, variant.IsDefault,
            variant.CreatedAtUtc);
        if (candidate.IsFailure)
        {
            return Result.Failure<long>(candidate.Error);
        }

        if (await variants.HasDifferentCurrencyAsync(
            command.MenuItemId, candidate.Value.Price.Currency, variant.Id,
            cancellationToken))
        {
            return Result.Failure<long>(
                MenuItemVariantApplicationErrors.CurrencyMismatch);
        }

        if (await variants.NameExistsAsync(
            command.MenuItemId, candidate.Value.Name, variant.Id, cancellationToken))
        {
            return Result.Failure<long>(MenuItemVariantApplicationErrors.DuplicateName);
        }

        var update = variant.Update(command.Name, command.Description,
            command.PriceAmount, command.Currency, command.DisplayOrder);
        if (update.IsFailure)
        {
            return Result.Failure<long>(update.Error);
        }

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
            await cacheInvalidator.InvalidateRestaurantAsync(
                item.RestaurantId, cancellationToken);
        }

        return Result.Success(variant.Version);
    }
}
