using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;

public sealed class DeleteMenuCategoryCommandHandler
    : ICommandHandler<
        DeleteMenuCategoryCommand,
        Result<MenuCategoryId>>
{
    private readonly IMenuCategoryRepository _repository;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IPublicMenuCacheInvalidator _cacheInvalidator;

    public DeleteMenuCategoryCommandHandler(
        IMenuCategoryRepository repository,
        ICatalogUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IPublicMenuCacheInvalidator cacheInvalidator)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(cacheInvalidator);
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<MenuCategoryId>> Handle(
        DeleteMenuCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await _repository.GetByIdAsync(
            command.CategoryId,
            cancellationToken);

        if (category is null ||
            category.RestaurantId != command.RestaurantId)
        {
            return Result.Failure<MenuCategoryId>(
                MenuCategoryApplicationErrors.CategoryNotFound(
                    command.CategoryId));
        }

        if (category.Version != command.ExpectedVersion)
        {
            return Result.Failure<MenuCategoryId>(
                MenuCategoryApplicationErrors.VersionConflict(
                    category.Id));
        }

        if (await _repository.HasChildrenAsync(
                category.Id,
                cancellationToken))
        {
            return Result.Failure<MenuCategoryId>(
                MenuCategoryApplicationErrors.HasChildren(
                    category.Id));
        }

        var wasPublished = category.IsPublished;
        category.Delete(_timeProvider.GetUtcNow());

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<MenuCategoryId>(
                MenuCategoryApplicationErrors.VersionConflict(
                    category.Id));
        }

        if (wasPublished)
        {
            await _cacheInvalidator.InvalidateRestaurantAsync(
                category.RestaurantId, cancellationToken);
        }

        return Result.Success(category.Id);
    }
}
