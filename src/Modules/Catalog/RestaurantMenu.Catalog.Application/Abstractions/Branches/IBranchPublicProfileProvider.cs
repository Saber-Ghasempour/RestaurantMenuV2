namespace RestaurantMenu.Catalog.Application.Abstractions.Branches;

public interface IBranchPublicProfileProvider
{
    Task<BranchPublicProfile?> GetAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);
}

public sealed record BranchPublicProfile(
    Guid RestaurantId,
    string RestaurantName,
    Guid BranchId,
    string BranchName,
    string? RestaurantSlug,
    string? BranchSlug,
    string? AddressLine,
    string? CityName,
    string? RegionName,
    string? PostalCode,
    string? CountryCode,
    string? Description = null,
    string? About = null,
    string? RestaurantAddress = null,
    string? WebsiteUrl = null,
    string? InstagramUrl = null,
    string? FacebookUrl = null,
    string? WhatsAppUrl = null,
    string? TelegramUrl = null,
    string? TwitterUrl = null);
