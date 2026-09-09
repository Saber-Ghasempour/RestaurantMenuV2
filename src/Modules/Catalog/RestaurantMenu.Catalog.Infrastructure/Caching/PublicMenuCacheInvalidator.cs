using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Caching;

public sealed class PublicMenuCacheInvalidator(
    CatalogDbContext dbContext,
    IPublicBranchMenuCache cache) : IPublicMenuCacheInvalidator
{
    public async Task InvalidateRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var branchIds = await dbContext.BranchCategoryPublications
            .AsNoTracking()
            .Where(publication => publication.RestaurantId == restaurantId)
            .Select(publication => publication.BranchId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        foreach (var branchId in branchIds)
        {
            await cache.RemoveAsync(restaurantId, branchId, cancellationToken);
        }
    }
}
