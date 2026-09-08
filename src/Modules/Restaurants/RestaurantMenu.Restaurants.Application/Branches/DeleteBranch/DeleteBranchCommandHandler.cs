using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.DeleteBranch;

public sealed class DeleteBranchCommandHandler(
    IBranchRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DeleteBranchCommand, Result<BranchId>>
{
    public async Task<Result<BranchId>> Handle(
        DeleteBranchCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var branch = await repository.GetByIdAsync(
            command.RestaurantId, command.BranchId, cancellationToken);
        if (branch is null)
        {
            return Result.Failure<BranchId>(BranchErrors.NotFound(command.BranchId));
        }

        if (branch.Version != command.ExpectedVersion)
        {
            return Result.Failure<BranchId>(BranchErrors.VersionConflict(branch.Id));
        }

        branch.Delete(timeProvider.GetUtcNow());
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<BranchId>(BranchErrors.VersionConflict(branch.Id));
        }

        return Result.Success(branch.Id);
    }
}
