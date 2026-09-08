using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.ListBranches;

public sealed class ListBranchesQueryHandler(IBranchReadService readService)
    : IQueryHandler<ListBranchesQuery, Result<BranchesPage>>
{
    public async Task<Result<BranchesPage>> Handle(
        ListBranchesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1)
        {
            return Result.Failure<BranchesPage>(ErrorDetail.Validation(
                "Branches.InvalidPage", "Page must be greater than zero."));
        }

        if (query.PageSize is < 1 or > ListBranchesQuery.MaxPageSize)
        {
            return Result.Failure<BranchesPage>(ErrorDetail.Validation(
                "Branches.InvalidPageSize", "Page size must be between 1 and 100."));
        }

        var page = await readService.GetPageAsync(
            query.RestaurantId, query.Page, query.PageSize,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.IsActive, cancellationToken);
        return Result.Success(page);
    }
}
