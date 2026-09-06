using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
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

    public DeleteMenuCategoryCommandHandler(
        IMenuCategoryRepository repository,
        ICatalogUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
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

        return Result.Success(category.Id);
    }
}
