using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Branches.GetBranch;
using RestaurantMenu.Restaurants.Application.Branches.ListBranches;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Branches;

public sealed class BranchReadService(RestaurantsDbContext dbContext)
    : IBranchReadService
{
    public Task<BranchResponse?> GetByIdAsync(
        RestaurantId restaurantId,
        BranchId branchId,
        CancellationToken cancellationToken) =>
        Project(dbContext.Branches.AsNoTracking().Where(branch =>
            branch.RestaurantId == restaurantId && branch.Id == branchId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<BranchesPage> GetPageAsync(
        RestaurantId restaurantId,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Branches.AsNoTracking()
            .Where(branch => branch.RestaurantId == restaurantId);
        if (search is not null)
        {
            query = query.Where(branch => EF.Functions.ILike(branch.Name, $"%{search}%"));
        }

        if (isActive.HasValue)
        {
            query = query.Where(branch => branch.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await Project(query
                .OrderBy(branch => branch.Name)
                .ThenBy(branch => branch.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToArrayAsync(cancellationToken);
        return new BranchesPage(items, page, pageSize, totalCount);
    }

    private static IQueryable<BranchResponse> Project(IQueryable<Branch> query) =>
        query.Select(branch => new BranchResponse(
            branch.Id.Value, branch.RestaurantId.Value, branch.Name, branch.Slug,
            branch.Phone, branch.AddressLine, branch.CityName, branch.RegionName,
            branch.PostalCode, branch.CountryCode, branch.Latitude, branch.Longitude,
            branch.TimeZoneId, branch.IsActive, branch.CreatedAtUtc, branch.Version));
}
