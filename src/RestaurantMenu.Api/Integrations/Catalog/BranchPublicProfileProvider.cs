using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Catalog;

public sealed class BranchPublicProfileProvider(
    IRestaurantReadService restaurantReadService,
    IBranchReadService branchReadService)
    : IBranchPublicProfileProvider
{
    public async Task<BranchPublicProfile?> GetAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var branch = await branchReadService.GetByIdAsync(
            new RestaurantId(restaurantId),
            new BranchId(branchId),
            cancellationToken);
        if (branch is null || !branch.IsActive)
        {
            return null;
        }

        var restaurant = await restaurantReadService.GetByIdAsync(
            new RestaurantId(restaurantId),
            cancellationToken);
        return restaurant is null
            ? null
            : new BranchPublicProfile(
                restaurant.Id,
                restaurant.Name,
                branch.Id,
                branch.Name,
                restaurant.Slug,
                branch.Slug,
                branch.AddressLine,
                branch.CityName,
                branch.RegionName,
                branch.PostalCode,
                branch.CountryCode,
                restaurant.Description,
                restaurant.About,
                restaurant.Address,
                restaurant.WebsiteUrl,
                restaurant.InstagramUrl,
                restaurant.FacebookUrl,
                restaurant.WhatsAppUrl,
                restaurant.TelegramUrl,
                restaurant.TwitterUrl,
                restaurant.DefaultCurrency,
                restaurant.DefaultLocale,
                branch.TimeZoneId ?? restaurant.TimeZoneId);
    }
}
