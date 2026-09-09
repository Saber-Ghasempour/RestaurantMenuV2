using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Media;

public sealed class MediaAssetIntegrationService(IMediaAssetReadService media,
    RestaurantsDbContext restaurants, CatalogDbContext catalog) :
    RestaurantMenu.Restaurants.Application.Abstractions.Media.IMediaAssetValidator,
    RestaurantMenu.Catalog.Application.Abstractions.Media.IMediaAssetValidator,
    IMediaReferenceChecker,
    IPublicMediaReferenceChecker
{
    public async Task<bool> IsReadyAsync(Guid restaurantId, Guid mediaAssetId, CancellationToken cancellationToken) =>
        (await media.GetAsync(restaurantId, new MediaAssetId(mediaAssetId), cancellationToken))?.Status == nameof(MediaAssetStatus.Ready);

    public async Task<bool> IsReferencedAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken)
    {
        var value = id.Value;
        var tenantId = new RestaurantId(restaurantId);
        return await restaurants.Restaurants.IgnoreQueryFilters().AnyAsync(x => x.Id == tenantId &&
                   (x.LogoMediaId == value || x.CoverMediaId == value), cancellationToken) ||
               await catalog.MenuCategories.IgnoreQueryFilters().AnyAsync(x => x.RestaurantId == restaurantId && x.ImageMediaId == value, cancellationToken) ||
               await catalog.MenuItemMedia.AnyAsync(x => x.RestaurantId == restaurantId && x.MediaAssetId == value, cancellationToken);
    }

    public async Task<bool> IsPubliclyReferencedAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken)
    {
        var value = id.Value;
        var tenantId = new RestaurantId(restaurantId);
        if (await restaurants.Restaurants.AnyAsync(x => x.Id == tenantId &&
            (x.LogoMediaId == value || x.CoverMediaId == value), cancellationToken)) return true;
        if (await catalog.MenuCategories.AnyAsync(x => x.RestaurantId == restaurantId && x.IsPublished && x.ImageMediaId == value, cancellationToken)) return true;
        return await (from link in catalog.MenuItemMedia
            join item in catalog.MenuItems on new { link.RestaurantId, link.MenuItemId } equals new { item.RestaurantId, MenuItemId = item.Id }
            join category in catalog.MenuCategories on new { item.RestaurantId, item.CategoryId } equals new { category.RestaurantId, CategoryId = category.Id }
            where link.RestaurantId == restaurantId && link.MediaAssetId == value && item.IsPublished && category.IsPublished
            select link).AnyAsync(cancellationToken);
    }
}
