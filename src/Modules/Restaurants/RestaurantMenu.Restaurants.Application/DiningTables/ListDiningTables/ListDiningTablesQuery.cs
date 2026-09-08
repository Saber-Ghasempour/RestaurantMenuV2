using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.ListDiningTables;
public sealed record ListDiningTablesQuery(RestaurantId RestaurantId, BranchId BranchId)
    : IQuery<Result<IReadOnlyList<DiningTableResponse>>>;
public sealed class ListDiningTablesQueryHandler(IDiningTableReadService readService, IBranchRepository branches)
    : IQueryHandler<ListDiningTablesQuery, Result<IReadOnlyList<DiningTableResponse>>>
{
    public async Task<Result<IReadOnlyList<DiningTableResponse>>> Handle(ListDiningTablesQuery query, CancellationToken cancellationToken)
    {
        if (await branches.GetByIdAsync(query.RestaurantId, query.BranchId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<DiningTableResponse>>(BranchErrors.NotFound(query.BranchId));
        return Result.Success(await readService.ListAsync(query.RestaurantId, query.BranchId, cancellationToken));
    }
}
