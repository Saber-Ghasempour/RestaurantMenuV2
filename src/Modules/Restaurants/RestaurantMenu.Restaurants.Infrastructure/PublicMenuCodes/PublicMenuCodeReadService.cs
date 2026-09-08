using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.PublicMenuCodes;
public sealed class PublicMenuCodeReadService(RestaurantsDbContext dbContext) : IPublicMenuCodeReadService
{
    public async Task<IReadOnlyList<PublicMenuCodeResponse>> ListAsync(RestaurantId restaurantId,
        CancellationToken cancellationToken) => await dbContext.PublicMenuCodes.AsNoTracking()
        .Where(code => code.RestaurantId == restaurantId)
        .OrderByDescending(code => code.CreatedAtUtc).ThenBy(code => code.Id)
        .Select(code => new PublicMenuCodeResponse(code.Id.Value, code.RestaurantId.Value,
            code.BranchId.HasValue ? code.BranchId.Value.Value : null,
            code.DiningTableId.HasValue ? code.DiningTableId.Value.Value : null,
            code.Purpose, code.IsActive, code.ExpiresAtUtc, code.CreatedAtUtc,
            code.RotatedAtUtc, code.Version)).ToArrayAsync(cancellationToken);

    public Task<ResolvedPublicMenuCode?> ResolveAsync(string codeHash, DateTimeOffset utcNow,
        CancellationToken cancellationToken) => dbContext.PublicMenuCodes.AsNoTracking()
        .Where(code => code.CodeHash == codeHash && code.IsActive &&
            (!code.ExpiresAtUtc.HasValue || code.ExpiresAtUtc > utcNow) &&
            (!code.BranchId.HasValue || dbContext.Branches.Any(branch =>
                branch.RestaurantId == code.RestaurantId && branch.Id == code.BranchId.Value && branch.IsActive)) &&
            (!code.DiningTableId.HasValue || dbContext.DiningTables.Any(table =>
                table.RestaurantId == code.RestaurantId && table.BranchId == code.BranchId!.Value &&
                table.Id == code.DiningTableId.Value && table.IsActive)))
        .Select(code => new ResolvedPublicMenuCode(code.RestaurantId.Value,
            code.BranchId.HasValue ? code.BranchId.Value.Value : null,
            code.DiningTableId.HasValue ? code.DiningTableId.Value.Value : null, code.Purpose))
        .SingleOrDefaultAsync(cancellationToken);
}
