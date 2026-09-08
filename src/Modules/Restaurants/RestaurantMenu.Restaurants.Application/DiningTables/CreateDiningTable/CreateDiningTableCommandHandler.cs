using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.CreateDiningTable;

public sealed class CreateDiningTableCommandHandler(IBranchRepository branches,
    IDiningTableRepository tables, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<CreateDiningTableCommand, Result<DiningTableId>>
{
    public async Task<Result<DiningTableId>> Handle(CreateDiningTableCommand command,
        CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(command.RestaurantId, command.BranchId, cancellationToken);
        if (branch is null) return Result.Failure<DiningTableId>(BranchErrors.NotFound(command.BranchId));
        if (!branch.IsActive) return Result.Failure<DiningTableId>(DiningTableErrors.BranchInactive);
        var created = DiningTable.Create(DiningTableId.New(), command.RestaurantId, command.BranchId,
            command.Number, command.DisplayName, command.Capacity, timeProvider.GetUtcNow());
        if (created.IsFailure) return Result.Failure<DiningTableId>(created.Error);
        tables.Add(created.Value);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DiningTableNumberAlreadyExistsException) { return Result.Failure<DiningTableId>(DiningTableErrors.NumberAlreadyExists); }
        return Result.Success(created.Value.Id);
    }
}
