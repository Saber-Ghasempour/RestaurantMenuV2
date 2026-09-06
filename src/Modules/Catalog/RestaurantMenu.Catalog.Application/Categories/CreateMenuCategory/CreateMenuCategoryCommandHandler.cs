using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;

public sealed class CreateMenuCategoryCommandHandler
    : ICommandHandler<
        CreateMenuCategoryCommand,
        Result<MenuCategoryId>>
{
    private readonly IMenuCategoryRepository _repository;
    private readonly IRestaurantExistenceChecker _restaurantChecker;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateMenuCategoryCommandHandler(
        IMenuCategoryRepository repository,
        IRestaurantExistenceChecker restaurantChecker,
        ICatalogUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(restaurantChecker);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _repository = repository;
        _restaurantChecker = restaurantChecker;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MenuCategoryId>> Handle(
        CreateMenuCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await _restaurantChecker.ExistsAsync(
                command.RestaurantId,
                cancellationToken))
        {
            return Result.Failure<MenuCategoryId>(
                MenuCategoryApplicationErrors.RestaurantNotFound(
                    command.RestaurantId));
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
                return Result.Failure<MenuCategoryId>(
                    MenuCategoryApplicationErrors.ParentCategoryNotFound(
                        command.ParentCategoryId!.Value));
            }
        }

        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            command.RestaurantId,
            parentId,
            command.Name,
            command.DisplayOrder,
            _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<MenuCategoryId>(result.Error);
        }

        _repository.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
