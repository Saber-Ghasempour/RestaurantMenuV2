using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

using System.Globalization;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed class Restaurant : AggregateRoot<RestaurantId>
{
    public const int MaxNameLength = 120;
    public const string InitialDefaultCurrency = "USD";
    public const string InitialDefaultLocale = "en-US";
    public const string InitialTimeZoneId = "Etc/UTC";

    private static readonly HashSet<string> KnownCurrencies = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
        .ToHashSet(StringComparer.Ordinal);

    private Restaurant(
        RestaurantId id,
        string name,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        CreatedAtUtc = createdAtUtc;
        DefaultCurrency = InitialDefaultCurrency;
        DefaultLocale = InitialDefaultLocale;
        TimeZoneId = InitialTimeZoneId;
    }

    public string Name { get; private set; }

    public string? Slug { get; private set; }

    public string DefaultCurrency { get; private set; }

    public string DefaultLocale { get; private set; }

    public string TimeZoneId { get; private set; }

    public Result<Restaurant> ChangeSlug(string? slug)
    {
        slug = slug?.Trim().ToLowerInvariant();
        if (slug is null || slug.Length is < 3 or > 80 ||
            slug[0] == '-' || slug[^1] == '-' ||
            slug.Contains("--", StringComparison.Ordinal) ||
            slug.Any(character => character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))
        {
            return Result.Failure<Restaurant>(ErrorDetail.Validation(
                "Restaurants.InvalidSlug",
                "Slug must contain 3 to 80 ASCII letters, digits or single hyphens, with no leading or trailing hyphen."));
        }

        if (Slug == slug)
        {
            return Result.Success(this);
        }

        Slug = slug;
        Version++;
        RaiseDomainEvent(new RestaurantSlugChangedDomainEvent(Id));
        return Result.Success(this);
    }

    public Result<Restaurant> UpdateDefaults(
        string? defaultCurrency,
        string? defaultLocale,
        string? timeZoneId)
    {
        var normalizedCurrency = defaultCurrency?.Trim().ToUpperInvariant();
        if (normalizedCurrency is null || !KnownCurrencies.Contains(normalizedCurrency))
        {
            return Result.Failure<Restaurant>(RestaurantErrors.InvalidDefaultCurrency);
        }

        string normalizedLocale;
        try
        {
            normalizedLocale = CultureInfo.GetCultureInfo(defaultLocale?.Trim() ?? string.Empty).Name;
        }
        catch (CultureNotFoundException)
        {
            return Result.Failure<Restaurant>(RestaurantErrors.InvalidDefaultLocale);
        }

        if (string.IsNullOrWhiteSpace(defaultLocale) ||
            normalizedLocale.Length is 0 or > 16)
        {
            return Result.Failure<Restaurant>(RestaurantErrors.InvalidDefaultLocale);
        }

        var normalizedTimeZone = timeZoneId?.Trim();
        if (string.IsNullOrEmpty(normalizedTimeZone) ||
            normalizedTimeZone.Length > 64 ||
            !normalizedTimeZone.Contains('/', StringComparison.Ordinal) ||
            !TimeZoneInfo.TryFindSystemTimeZoneById(normalizedTimeZone, out _))
        {
            return Result.Failure<Restaurant>(RestaurantErrors.InvalidTimeZone);
        }

        if (DefaultCurrency == normalizedCurrency &&
            DefaultLocale == normalizedLocale &&
            TimeZoneId == normalizedTimeZone)
        {
            return Result.Success(this);
        }

        DefaultCurrency = normalizedCurrency;
        DefaultLocale = normalizedLocale;
        TimeZoneId = normalizedTimeZone;
        Version++;
        RaiseDomainEvent(new RestaurantDefaultsUpdatedDomainEvent(Id));
        return Result.Success(this);
    }

    public DateTimeOffset CreatedAtUtc { get; }

    public long Version { get; private set; } = 1;

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? WebsiteUrl { get; private set; }
    public string? InstagramUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? WhatsAppUrl { get; private set; }
    public string? TelegramUrl { get; private set; }
    public string? TwitterUrl { get; private set; }

    public string? Description { get; private set; }
    public string? About { get; private set; }
    public string? Address { get; private set; }

    public const int MaxDescriptionLength = 500;
    public const int MaxAboutLength = 4000;
    public const int MaxAddressLength = 500;

    private static string? NormalizeProfileText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Result<Restaurant> Create(
        RestaurantId id,
        string? name,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameTooLong);
        }

        var restaurant = new Restaurant(
            id,
            normalizedName,
            createdAtUtc);

        restaurant.RaiseDomainEvent(
            new RestaurantCreatedDomainEvent(id));

        return Result.Success(restaurant);
    }

    public Result<Restaurant> Rename(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<Restaurant>(
                RestaurantErrors.NameTooLong);
        }

        if (Name == normalizedName)
        {
            return Result.Success(this);
        }

        Name = normalizedName;
        Version++;

        RaiseDomainEvent(
            new RestaurantRenamedDomainEvent(Id));

        return Result.Success(this);
    }

    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        Version++;

        RaiseDomainEvent(
            new RestaurantDeletedDomainEvent(
                Id,
                deletedAtUtc));
    }

    public Result<Restaurant> UpdateLinks(
        string? websiteUrl,
        string? instagramUrl,
        string? facebookUrl,
        string? whatsAppUrl,
        string? telegramUrl,
        string? twitterUrl)
    {
        websiteUrl = NormalizeProfileText(websiteUrl);
        instagramUrl = NormalizeProfileText(instagramUrl);
        facebookUrl = NormalizeProfileText(facebookUrl);
        whatsAppUrl = NormalizeProfileText(whatsAppUrl);
        telegramUrl = NormalizeProfileText(telegramUrl);
        twitterUrl = NormalizeProfileText(twitterUrl);
        var links = new[] { websiteUrl, instagramUrl, facebookUrl, whatsAppUrl, telegramUrl, twitterUrl };
        if (links.Any(link => !PublicWebsiteLink.IsValid(link)))
        {
            return Result.Failure<Restaurant>(
                ErrorDetail.Validation("Restaurants.InvalidLink",
                    "Links must be absolute HTTPS URLs without credentials or whitespace, up to 2048 characters."));
        }

        if (WebsiteUrl == websiteUrl &&
            InstagramUrl == instagramUrl &&
            FacebookUrl == facebookUrl &&
            WhatsAppUrl == whatsAppUrl &&
            TelegramUrl == telegramUrl &&
            TwitterUrl == twitterUrl)
        {
            return Result.Success(this);
        }

        WebsiteUrl = websiteUrl;
        InstagramUrl = instagramUrl;
        FacebookUrl = facebookUrl;
        WhatsAppUrl = whatsAppUrl;
        TelegramUrl = telegramUrl;
        TwitterUrl = twitterUrl;
        Version++;
        RaiseDomainEvent(new RestaurantLinksUpdatedDomainEvent(Id));
        return Result.Success(this);
    }

    public Result<Restaurant> UpdateProfile(
        string? description,
        string? about,
        string? address)
    {
        description = NormalizeProfileText(description);
        about = NormalizeProfileText(about);
        address = NormalizeProfileText(address);

        if (description?.Length > MaxDescriptionLength ||
            about?.Length > MaxAboutLength ||
            address?.Length > MaxAddressLength)
        {
            return Result.Failure<Restaurant>(
                ErrorDetail.Validation(
                    "Restaurants.ProfileTooLong",
                    "Description and address must not exceed 500 characters; about must not exceed 4000 characters."));
        }

        if (Description == description && About == about && Address == address)
        {
            return Result.Success(this);
        }

        Description = description;
        About = about;
        Address = address;
        Version++;
        RaiseDomainEvent(new RestaurantProfileUpdatedDomainEvent(Id));
        return Result.Success(this);
    }
}
