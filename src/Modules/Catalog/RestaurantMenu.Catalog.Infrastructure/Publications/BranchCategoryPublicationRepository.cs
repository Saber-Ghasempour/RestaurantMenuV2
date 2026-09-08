using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Publications;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Publications;

public sealed class BranchCategoryPublicationRepository(CatalogDbContext dbContext)
    : IBranchCategoryPublicationRepository
{
    public async Task<IReadOnlyList<PublicationCategory>> GetCategoriesAsync(
        Guid restaurantId,
        CancellationToken cancellationToken) =>
        await dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => category.RestaurantId == restaurantId)
            .Select(category => new PublicationCategory(
                category.Id,
                category.ParentId))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<BranchCategoryPublication>> GetByBranchAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken) =>
        await dbContext.BranchCategoryPublications
            .Where(publication =>
                publication.RestaurantId == restaurantId &&
                publication.BranchId == branchId)
            .ToArrayAsync(cancellationToken);

    public void Add(BranchCategoryPublication publication) =>
        dbContext.BranchCategoryPublications.Add(publication);
}
