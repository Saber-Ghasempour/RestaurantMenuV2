using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.ChangeMenuItemPublication;

public sealed class ChangeMenuItemPublicationCommandHandler(
    IMenuItemRepository repository,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<ChangeMenuItemPublicationCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        ChangeMenuItemPublicationCommand command,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(command.MenuItemId, cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId ||
            item.CategoryId != command.CategoryId)
        {
            return Result.Failure<long>(MenuItemApplicationErrors.ItemNotFound(command.MenuItemId));
        }

        if (item.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(MenuItemApplicationErrors.VersionConflict(item.Id));
        }

        var previousVersion = item.Version;
        item.ChangePublication(command.IsPublished);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(MenuItemApplicationErrors.VersionConflict(item.Id));
        }

        if (item.Version != previousVersion)
        {
            await cacheInvalidator.InvalidateRestaurantAsync(command.RestaurantId, cancellationToken);
        }

        return Result.Success(item.Version);
    }
}
