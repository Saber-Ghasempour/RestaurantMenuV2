using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;

public sealed class UpdateMenuItemCommandHandler
    : ICommandHandler<UpdateMenuItemCommand, Result<long>>
{
    private readonly IMenuItemRepository _repository;
    private readonly ICatalogUnitOfWork _unitOfWork;

    public UpdateMenuItemCommandHandler(
        IMenuItemRepository repository,
        ICatalogUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(
        UpdateMenuItemCommand command,
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
            return Result.Failure<long>(
                MenuItemApplicationErrors.ItemNotFound(
                    command.MenuItemId));
        }

        if (menuItem.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(
                MenuItemApplicationErrors.VersionConflict(
                    menuItem.Id));
        }

        var updateResult = menuItem.Update(
            command.Name,
            command.Description,
            command.PriceAmount,
            command.Currency,
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
                MenuItemApplicationErrors.VersionConflict(
                    menuItem.Id));
        }

        return Result.Success(menuItem.Version);
    }
}
