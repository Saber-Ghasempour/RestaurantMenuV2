using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Memberships.AssignBranchMembership;

public sealed class AssignBranchMembershipCommandHandler(IBranchRepository branches,
    IRestaurantMembershipReadService restaurantMembers, IBranchMembershipRepository branchMembers,
    IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<AssignBranchMembershipCommand, Result<BranchMembershipResponse>>
{
    public async Task<Result<BranchMembershipResponse>> Handle(AssignBranchMembershipCommand command,
        CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(command.RestaurantId, command.BranchId, cancellationToken);
        if (branch is null) return Result.Failure<BranchMembershipResponse>(
            BranchMembershipApplicationErrors.BranchNotFound(command.BranchId));
        if (!await restaurantMembers.HasAccessAsync(command.RestaurantId, command.Subject, cancellationToken))
            return Result.Failure<BranchMembershipResponse>(BranchMembershipApplicationErrors.RestaurantMemberRequired);
        var membership = await branchMembers.GetAsync(command.RestaurantId, command.BranchId,
            command.Subject.Trim(), cancellationToken);
        if (membership is null)
        {
            if (command.ExpectedVersion is > 0)
                return Result.Failure<BranchMembershipResponse>(BranchMembershipApplicationErrors.VersionConflict);
            var created = BranchMembership.Create(command.RestaurantId, command.BranchId,
                command.Subject, command.Role, timeProvider.GetUtcNow(), command.Status);
            if (created.IsFailure) return Result.Failure<BranchMembershipResponse>(created.Error);
            membership = created.Value;
            branchMembers.Add(membership);
        }
        else
        {
            if (command.ExpectedVersion != membership.Version)
                return Result.Failure<BranchMembershipResponse>(BranchMembershipApplicationErrors.VersionConflict);
            var changed = membership.Change(command.Role, command.Status);
            if (changed.IsFailure) return Result.Failure<BranchMembershipResponse>(changed.Error);
        }
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException)
        { return Result.Failure<BranchMembershipResponse>(BranchMembershipApplicationErrors.VersionConflict); }
        return Result.Success(new BranchMembershipResponse(membership.RestaurantId.Value,
            membership.BranchId.Value, membership.Subject, membership.Role.ToString(),
            membership.Status.ToString(), membership.Version));
    }
}
