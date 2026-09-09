using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants.CreateMenuItemVariant;

public sealed class CreateMenuItemVariantCommandHandler(
    IMenuItemRepository menuItems,
    IMenuItemVariantRepository variants,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator,
    TimeProvider timeProvider)
    : ICommandHandler<CreateMenuItemVariantCommand, Result<MenuItemVariantId>>
{
    public async Task<Result<MenuItemVariantId>> Handle(
        CreateMenuItemVariantCommand command,
        CancellationToken cancellationToken)
    {
        var item = await menuItems.GetByIdAsync(command.MenuItemId, cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId ||
            item.CategoryId != command.CategoryId)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.MenuItemNotFound(command.MenuItemId));
        }

        var result = MenuItemVariant.Create(
            MenuItemVariantId.New(), command.RestaurantId, command.MenuItemId,
            command.Name, command.Description, command.PriceAmount, command.Currency,
            command.DisplayOrder, command.IsDefault, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return Result.Failure<MenuItemVariantId>(result.Error);
        }

        var variant = result.Value;
        if (await variants.NameExistsAsync(
            command.MenuItemId, variant.Name, null, cancellationToken))
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.DuplicateName);
        }

        var currentDefault = await variants.GetDefaultAsync(
            command.MenuItemId, cancellationToken);
        if (currentDefault is null && !command.IsDefault)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.DefaultRequired);
        }

        if (currentDefault is not null &&
            currentDefault.Price.Currency != variant.Price.Currency)
        {
            return Result.Failure<MenuItemVariantId>(
                MenuItemVariantApplicationErrors.CurrencyMismatch);
        }

        if (command.IsDefault)
        {
            currentDefault?.RemoveDefault();
        }

        variants.Add(variant);
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
            await cacheInvalidator.InvalidateRestaurantAsync(
                item.RestaurantId, cancellationToken);
        }

        return Result.Success(variant.Id);
    }
}
