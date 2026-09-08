using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Publications;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Publications.SetBranchCategoryPublications;

public sealed class SetBranchCategoryPublicationsCommandHandler(
    IBranchExistenceChecker branchChecker,
    IBranchCategoryPublicationRepository repository,
    ICatalogUnitOfWork unitOfWork,
    IPublicBranchMenuCache cache,
    TimeProvider timeProvider)
    : ICommandHandler<SetBranchCategoryPublicationsCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        SetBranchCategoryPublicationsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Publications);

        if (!await branchChecker.ExistsAsync(
                command.RestaurantId,
                command.BranchId,
                cancellationToken))
        {
            return Result.Failure<bool>(
                BranchCategoryPublicationErrors.BranchNotFound(
                    command.BranchId));
        }

        var categories = await repository.GetCategoriesAsync(
            command.RestaurantId,
            cancellationToken);
        var requestedIds = command.Publications
            .Select(publication => publication.CategoryId)
            .ToArray();
        var categoryIds = categories
            .Select(category => category.Id.Value)
            .ToHashSet();

        if (requestedIds.Length != categoryIds.Count ||
            requestedIds.Distinct().Count() != requestedIds.Length ||
            requestedIds.Any(categoryId => !categoryIds.Contains(categoryId)))
        {
            return Result.Failure<bool>(
                BranchCategoryPublicationErrors.CompleteSetRequired);
        }

        var inputByCategory = command.Publications.ToDictionary(
            publication => publication.CategoryId);
        foreach (var category in categories.Where(category => category.ParentId.HasValue))
        {
            var input = inputByCategory[category.Id.Value];
            if (input.IsPublished &&
                !inputByCategory[category.ParentId!.Value.Value].IsPublished)
            {
                return Result.Failure<bool>(
                    BranchCategoryPublicationErrors.PublishedParentRequired(
                        category.Id.Value));
            }
        }

        var existing = await repository.GetByBranchAsync(
            command.RestaurantId,
            command.BranchId,
            cancellationToken);
        var existingByCategory = existing.ToDictionary(
            publication => publication.CategoryId.Value);
        var changed = false;

        foreach (var input in command.Publications)
        {
            if (!existingByCategory.TryGetValue(input.CategoryId, out var publication))
            {
                var createResult = BranchCategoryPublication.Create(
                    command.RestaurantId,
                    command.BranchId,
                    new MenuCategoryId(input.CategoryId),
                    input.IsPublished,
                    input.DisplayOrderOverride,
                    timeProvider.GetUtcNow());
                if (createResult.IsFailure)
                {
                    return Result.Failure<bool>(createResult.Error);
                }

                repository.Add(createResult.Value);
                changed = true;
                continue;
            }

            var previousVersion = publication.Version;
            var setResult = publication.Set(
                input.IsPublished,
                input.DisplayOrderOverride);
            if (setResult.IsFailure)
            {
                return Result.Failure<bool>(setResult.Error);
            }

            changed |= publication.Version != previousVersion;
        }

        if (!changed)
        {
            return Result.Success(false);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<bool>(
                BranchCategoryPublicationErrors.VersionConflict);
        }

        await cache.RemoveAsync(
            command.RestaurantId,
            command.BranchId,
            cancellationToken);

        return Result.Success(true);
    }
}
