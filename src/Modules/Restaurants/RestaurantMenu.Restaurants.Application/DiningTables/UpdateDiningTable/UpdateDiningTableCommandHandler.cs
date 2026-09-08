using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.UpdateDiningTable;
public sealed class UpdateDiningTableCommandHandler(IDiningTableRepository tables, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateDiningTableCommand, Result<long>>
{
    public async Task<Result<long>> Handle(UpdateDiningTableCommand command, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(command.RestaurantId, command.BranchId, command.DiningTableId, cancellationToken);
        if (table is null) return Result.Failure<long>(DiningTableErrors.NotFound(command.DiningTableId));
        if (table.Version != command.ExpectedVersion) return Result.Failure<long>(DiningTableErrors.VersionConflict(table.Id));
        var updated = table.Update(command.Number, command.DisplayName, command.Capacity);
        if (updated.IsFailure) return Result.Failure<long>(updated.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DiningTableNumberAlreadyExistsException) { return Result.Failure<long>(DiningTableErrors.NumberAlreadyExists); }
        catch (ConcurrencyException) { return Result.Failure<long>(DiningTableErrors.VersionConflict(table.Id)); }
        return Result.Success(table.Version);
    }
}
