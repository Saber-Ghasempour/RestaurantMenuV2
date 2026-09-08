using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.ChangeBranchStatus;

public sealed class ChangeBranchStatusCommandHandler(
    IBranchRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeBranchStatusCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        ChangeBranchStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var branch = await repository.GetByIdAsync(
            command.RestaurantId, command.BranchId, cancellationToken);
        if (branch is null)
        {
            return Result.Failure<long>(BranchErrors.NotFound(command.BranchId));
        }

        if (branch.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(BranchErrors.VersionConflict(branch.Id));
        }

        branch.ChangeStatus(command.IsActive);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(BranchErrors.VersionConflict(branch.Id));
        }

        return Result.Success(branch.Version);
    }
}
