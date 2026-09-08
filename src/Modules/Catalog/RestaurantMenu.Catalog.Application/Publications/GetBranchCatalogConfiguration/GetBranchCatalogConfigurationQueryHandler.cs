using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Publications.GetBranchCatalogConfiguration;

public sealed class GetBranchCatalogConfigurationQueryHandler(
    IBranchExistenceChecker branchChecker,
    IBranchCatalogReadService readService)
    : IQueryHandler<GetBranchCatalogConfigurationQuery,
        Result<IReadOnlyList<BranchCategoryPublicationResponse>>>
{
    public async Task<Result<IReadOnlyList<BranchCategoryPublicationResponse>>> Handle(
        GetBranchCatalogConfigurationQuery query,
        CancellationToken cancellationToken)
    {
        if (!await branchChecker.ExistsAsync(
                query.RestaurantId,
                query.BranchId,
                cancellationToken))
        {
            return Result.Failure<IReadOnlyList<BranchCategoryPublicationResponse>>(
                BranchCategoryPublicationErrors.BranchNotFound(query.BranchId));
        }

        return Result.Success(
            await readService.GetConfigurationAsync(
                query.RestaurantId,
                query.BranchId,
                cancellationToken));
    }
}
