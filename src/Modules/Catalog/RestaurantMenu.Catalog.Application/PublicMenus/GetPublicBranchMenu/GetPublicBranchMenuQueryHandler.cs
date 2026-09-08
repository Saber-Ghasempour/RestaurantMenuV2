using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;

public sealed class GetPublicBranchMenuQueryHandler(
    IBranchPublicProfileProvider profileProvider,
    IBranchCatalogReadService readService,
    IPublicBranchMenuCache cache)
    : IQueryHandler<GetPublicBranchMenuQuery, Result<PublicBranchMenuResponse>>
{
    public async Task<Result<PublicBranchMenuResponse>> Handle(
        GetPublicBranchMenuQuery query,
        CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(
            query.RestaurantId,
            query.BranchId,
            cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var profile = await profileProvider.GetAsync(
            query.RestaurantId,
            query.BranchId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Failure<PublicBranchMenuResponse>(
                BranchCategoryPublicationErrors.BranchNotFound(query.BranchId));
        }

        var categories = await readService.GetPublicMenuAsync(
            query.RestaurantId,
            query.BranchId,
            cancellationToken);
        var response = new PublicBranchMenuResponse(
            profile.RestaurantId,
            profile.RestaurantName,
            profile.BranchId,
            profile.BranchName,
            categories,
            profile.RestaurantSlug,
            profile.BranchSlug,
            profile.AddressLine,
            profile.CityName,
            profile.RegionName,
            profile.PostalCode,
            profile.CountryCode,
            profile.Description,
            profile.About,
            profile.RestaurantAddress,
            profile.WebsiteUrl,
            profile.InstagramUrl,
            profile.FacebookUrl,
            profile.WhatsAppUrl,
            profile.TelegramUrl,
            profile.TwitterUrl);

        await cache.SetAsync(response, cancellationToken);
        return Result.Success(response);
    }
}
