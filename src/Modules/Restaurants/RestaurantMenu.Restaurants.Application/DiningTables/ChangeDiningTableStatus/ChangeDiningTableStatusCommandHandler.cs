using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.ChangeDiningTableStatus;
public sealed class ChangeDiningTableStatusCommandHandler(IDiningTableRepository tables,
    IBranchRepository branches, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeDiningTableStatusCommand, Result<long>>
{
    public async Task<Result<long>> Handle(ChangeDiningTableStatusCommand command, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(command.RestaurantId, command.BranchId, command.DiningTableId, cancellationToken);
        if (table is null) return Result.Failure<long>(DiningTableErrors.NotFound(command.DiningTableId));
        if (table.Version != command.ExpectedVersion) return Result.Failure<long>(DiningTableErrors.VersionConflict(table.Id));
        if (command.IsActive)
        {
            var branch = await branches.GetByIdAsync(command.RestaurantId, command.BranchId, cancellationToken);
            if (branch is null) return Result.Failure<long>(BranchErrors.NotFound(command.BranchId));
            if (!branch.IsActive) return Result.Failure<long>(DiningTableErrors.BranchInactive);
        }
        table.ChangeStatus(command.IsActive);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException) { return Result.Failure<long>(DiningTableErrors.VersionConflict(table.Id)); }
        return Result.Success(table.Version);
    }
}
