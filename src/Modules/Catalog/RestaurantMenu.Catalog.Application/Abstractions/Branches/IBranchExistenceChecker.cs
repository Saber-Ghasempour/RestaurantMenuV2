namespace RestaurantMenu.Catalog.Application.Abstractions.Branches;

public interface IBranchExistenceChecker
{
    Task<bool> ExistsAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);
}
