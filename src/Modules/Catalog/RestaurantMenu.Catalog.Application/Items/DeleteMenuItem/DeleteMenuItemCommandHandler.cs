using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.DeleteMenuItem;

public sealed class DeleteMenuItemCommandHandler
    : ICommandHandler<DeleteMenuItemCommand, Result<MenuItemId>>
{
    private readonly IMenuItemRepository _repository;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public DeleteMenuItemCommandHandler(
        IMenuItemRepository repository,
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

    public async Task<Result<MenuItemId>> Handle(
        DeleteMenuItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var menuItem = await _repository.GetByIdAsync(
            command.MenuItemId,
            cancellationToken);

        if (menuItem is null ||
            menuItem.RestaurantId != command.RestaurantId ||
            menuItem.CategoryId != command.CategoryId)
        {
            return Result.Failure<MenuItemId>(
                MenuItemApplicationErrors.ItemNotFound(
                    command.MenuItemId));
        }

        if (menuItem.Version != command.ExpectedVersion)
        {
            return Result.Failure<MenuItemId>(
                MenuItemApplicationErrors.VersionConflict(
                    menuItem.Id));
        }

        menuItem.Delete(_timeProvider.GetUtcNow());

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<MenuItemId>(
                MenuItemApplicationErrors.VersionConflict(
                    menuItem.Id));
        }

        return Result.Success(menuItem.Id);
    }
}
