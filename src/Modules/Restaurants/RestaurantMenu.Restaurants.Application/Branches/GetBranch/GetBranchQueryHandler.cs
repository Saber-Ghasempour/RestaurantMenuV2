using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.GetBranch;

public sealed class GetBranchQueryHandler(IBranchReadService readService)
    : IQueryHandler<GetBranchQuery, Result<BranchResponse>>
{
    public async Task<Result<BranchResponse>> Handle(
        GetBranchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var branch = await readService.GetByIdAsync(
            query.RestaurantId, query.BranchId, cancellationToken);
        return branch is null
            ? Result.Failure<BranchResponse>(BranchErrors.NotFound(query.BranchId))
            : Result.Success(branch);
    }
}
