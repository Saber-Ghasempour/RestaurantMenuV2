using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategoryContent;

public sealed class UpdateMenuCategoryContentCommandHandler(
    IMenuCategoryRepository repository,
    ICatalogUnitOfWork unitOfWork,
    IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<UpdateMenuCategoryContentCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        UpdateMenuCategoryContentCommand command,
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

        var previousVersion = category.Version;
        var updateResult = category.UpdateContent(command.Description);
        if (updateResult.IsFailure)
        {
            return Result.Failure<long>(updateResult.Error);
        }

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
