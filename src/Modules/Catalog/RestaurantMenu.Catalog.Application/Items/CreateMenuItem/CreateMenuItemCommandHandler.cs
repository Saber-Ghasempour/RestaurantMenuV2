using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.CreateMenuItem;

public sealed class CreateMenuItemCommandHandler
    : ICommandHandler<CreateMenuItemCommand, Result<MenuItemId>>
{
    private readonly IMenuCategoryRepository _categoryRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRestaurantExistenceChecker _restaurantChecker;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateMenuItemCommandHandler(
        IMenuCategoryRepository categoryRepository,
        IMenuItemRepository menuItemRepository,
        IRestaurantExistenceChecker restaurantChecker,
        ICatalogUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(menuItemRepository);
        ArgumentNullException.ThrowIfNull(restaurantChecker);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _categoryRepository = categoryRepository;
        _menuItemRepository = menuItemRepository;
        _restaurantChecker = restaurantChecker;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MenuItemId>> Handle(
        CreateMenuItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await _restaurantChecker.ExistsAsync(
                command.RestaurantId,
                cancellationToken))
        {
            return Result.Failure<MenuItemId>(
                CatalogApplicationErrors.RestaurantNotFound(
                    command.RestaurantId));
        }

        var categoryId = new MenuCategoryId(command.CategoryId);
        var category = await _categoryRepository.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null ||
            category.RestaurantId != command.RestaurantId)
        {
            return Result.Failure<MenuItemId>(
                MenuItemApplicationErrors.CategoryNotFound(
                    command.CategoryId));
        }

        var menuItemResult = MenuItem.Create(
            MenuItemId.New(),
            command.RestaurantId,
            categoryId,
            command.Name,
            command.Description,
            command.PriceAmount,
            command.Currency,
            command.DisplayOrder,
            _timeProvider.GetUtcNow());

        if (menuItemResult.IsFailure)
        {
            return Result.Failure<MenuItemId>(
                menuItemResult.Error);
        }

        _menuItemRepository.Add(menuItemResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(menuItemResult.Value.Id);
    }
}
