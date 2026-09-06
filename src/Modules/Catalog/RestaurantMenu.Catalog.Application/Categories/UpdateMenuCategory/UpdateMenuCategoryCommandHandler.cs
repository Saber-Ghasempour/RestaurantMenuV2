using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;

public sealed class UpdateMenuCategoryCommandHandler
    : ICommandHandler<UpdateMenuCategoryCommand, Result<long>>
{
    private readonly IMenuCategoryRepository _repository;
    private readonly ICatalogUnitOfWork _unitOfWork;

    public UpdateMenuCategoryCommandHandler(
        IMenuCategoryRepository repository,
        ICatalogUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(
        UpdateMenuCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await _repository.GetByIdAsync(
            command.CategoryId,
            cancellationToken);

        if (category is null ||
            category.RestaurantId != command.RestaurantId)
        {
            return Result.Failure<long>(
                MenuCategoryApplicationErrors.CategoryNotFound(
                    command.CategoryId));
        }

        if (category.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(
                MenuCategoryApplicationErrors.VersionConflict(
                    category.Id));
        }

        MenuCategoryId? parentId = command.ParentCategoryId.HasValue
            ? new MenuCategoryId(command.ParentCategoryId.Value)
            : null;

        if (parentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(
                parentId.Value,
                cancellationToken);

            if (parent is null ||
                parent.RestaurantId != command.RestaurantId)
            {
                return Result.Failure<long>(
                    MenuCategoryApplicationErrors.ParentCategoryNotFound(
                        command.ParentCategoryId!.Value));
            }

            if (await WouldCreateCycleAsync(
                    category.Id,
                    parent,
                    cancellationToken))
            {
                return Result.Failure<long>(
                    MenuCategoryApplicationErrors.HierarchyCycle(
                        category.Id));
            }
        }

        var updateResult = category.Update(
            parentId,
            command.Name,
            command.DisplayOrder);

        if (updateResult.IsFailure)
        {
            return Result.Failure<long>(updateResult.Error);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(
                MenuCategoryApplicationErrors.VersionConflict(
                    category.Id));
        }

        return Result.Success(category.Version);
    }

    private async Task<bool> WouldCreateCycleAsync(
        MenuCategoryId categoryId,
        MenuCategory proposedParent,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<MenuCategoryId>();
        var current = proposedParent;

        while (true)
        {
            if (current.Id == categoryId ||
                !visited.Add(current.Id))
            {
                return true;
            }

            if (!current.ParentId.HasValue)
            {
                return false;
            }

            var ancestor = await _repository.GetByIdAsync(
                current.ParentId.Value,
                cancellationToken);

            if (ancestor is null)
            {
                return false;
            }

            current = ancestor;
        }
    }
}
