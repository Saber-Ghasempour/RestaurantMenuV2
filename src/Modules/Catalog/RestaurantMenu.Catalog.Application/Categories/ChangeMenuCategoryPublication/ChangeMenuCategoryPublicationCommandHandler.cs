using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.ChangeMenuCategoryPublication;

public sealed class ChangeMenuCategoryPublicationCommandHandler(
    IMenuCategoryRepository repository,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<ChangeMenuCategoryPublicationCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        ChangeMenuCategoryPublicationCommand command,
        CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null || category.RestaurantId != command.RestaurantId)
        {
            return Result.Failure<long>(MenuCategoryApplicationErrors.CategoryNotFound(command.CategoryId));
        }

        if (category.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(MenuCategoryApplicationErrors.VersionConflict(category.Id));
        }

        if (command.IsPublished && category.ParentId.HasValue)
        {
            var parent = await repository.GetByIdAsync(
                category.ParentId.Value, cancellationToken);
            if (parent is null || !parent.IsPublished)
            {
                return Result.Failure<long>(
                    MenuCategoryApplicationErrors.PublicationRequiresPublishedParent(category.Id));
            }
        }

        if (!command.IsPublished && category.IsPublished &&
            await repository.HasPublishedChildrenAsync(category.Id, cancellationToken))
        {
            return Result.Failure<long>(
                MenuCategoryApplicationErrors.HasPublishedChildren(category.Id));
        }

        var previousVersion = category.Version;
        category.ChangePublication(command.IsPublished);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(MenuCategoryApplicationErrors.VersionConflict(category.Id));
        }

        if (category.Version != previousVersion)
        {
            await cacheInvalidator.InvalidateRestaurantAsync(command.RestaurantId, cancellationToken);
        }

        return Result.Success(category.Version);
    }
}
