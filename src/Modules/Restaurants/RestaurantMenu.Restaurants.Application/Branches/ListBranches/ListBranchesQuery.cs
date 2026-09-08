using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Branches.GetBranch;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.ListBranches;

public sealed record ListBranchesQuery(
    RestaurantId RestaurantId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IQuery<Result<BranchesPage>>
{
    public const int MaxPageSize = 100;
}

public sealed record BranchesPage(
    IReadOnlyList<BranchResponse> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
