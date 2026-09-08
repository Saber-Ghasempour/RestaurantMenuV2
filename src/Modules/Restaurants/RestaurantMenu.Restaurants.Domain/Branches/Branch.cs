using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Branches;

public sealed class Branch : AggregateRoot<BranchId>
{
    public const int MaxNameLength = 120;
    public const int MaxSlugLength = 80;
    public const int MaxPhoneLength = 32;
    public const int MaxAddressLength = 500;
    public const int MaxLocationNameLength = 120;
    public const int MaxPostalCodeLength = 32;
    public const int MaxTimeZoneIdLength = 64;

    private Branch(
        BranchId id,
        RestaurantId restaurantId,
        BranchDetails details,
        DateTimeOffset createdAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        Apply(details);
        CreatedAtUtc = createdAtUtc;
    }

    private Branch(
        BranchId id,
        RestaurantId restaurantId,
        string name,
        string? slug,
        string? phone,
        string? addressLine,
        string? cityName,
        string? regionName,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        string? timeZoneId,
        bool isActive,
        DateTimeOffset createdAtUtc,
        long version,
        bool isDeleted,
        DateTimeOffset? deletedAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        Name = name;
        Slug = slug;
        Phone = phone;
        AddressLine = addressLine;
        CityName = cityName;
        RegionName = regionName;
        PostalCode = postalCode;
        CountryCode = countryCode;
        Latitude = latitude;
        Longitude = longitude;
        TimeZoneId = timeZoneId;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        Version = version;
        IsDeleted = isDeleted;
        DeletedAtUtc = deletedAtUtc;
    }

    public RestaurantId RestaurantId { get; }
    public string Name { get; private set; } = null!;
    public string? Slug { get; private set; }
    public string? Phone { get; private set; }
    public string? AddressLine { get; private set; }
    public string? CityName { get; private set; }
    public string? RegionName { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? TimeZoneId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<Branch> Create(
        BranchId id,
        RestaurantId restaurantId,
        string? name,
        string? slug,
        string? phone,
        string? addressLine,
        string? cityName,
        string? regionName,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        string? timeZoneId,
        DateTimeOffset createdAtUtc)
    {
        var detailsResult = Validate(
            name, slug, phone, addressLine, cityName, regionName, postalCode,
            countryCode, latitude, longitude, timeZoneId);

        if (detailsResult.IsFailure)
        {
            return Result.Failure<Branch>(detailsResult.Error);
        }

        var branch = new Branch(id, restaurantId, detailsResult.Value, createdAtUtc);
        branch.RaiseDomainEvent(new BranchCreatedDomainEvent(id, restaurantId));
        return Result.Success(branch);
    }

    public Result<Branch> Update(
        string? name,
        string? slug,
        string? phone,
        string? addressLine,
        string? cityName,
        string? regionName,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        string? timeZoneId)
    {
        var detailsResult = Validate(
            name, slug, phone, addressLine, cityName, regionName, postalCode,
            countryCode, latitude, longitude, timeZoneId);

        if (detailsResult.IsFailure)
        {
            return Result.Failure<Branch>(detailsResult.Error);
        }

        if (Matches(detailsResult.Value))
        {
            return Result.Success(this);
        }

        Apply(detailsResult.Value);
        Version++;
        RaiseDomainEvent(new BranchUpdatedDomainEvent(Id, RestaurantId));
        return Result.Success(this);
    }

    public void ChangeStatus(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Version++;
        RaiseDomainEvent(new BranchStatusChangedDomainEvent(Id, RestaurantId, isActive));
    }

    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = deletedAtUtc;
        Version++;
        RaiseDomainEvent(new BranchDeletedDomainEvent(Id, RestaurantId, deletedAtUtc));
    }

    private static Result<BranchDetails> Validate(
        string? name,
        string? slug,
        string? phone,
        string? addressLine,
        string? cityName,
        string? regionName,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<BranchDetails>(BranchErrors.NameRequired);
        }

        name = name.Trim();
        if (name.Length > MaxNameLength)
        {
            return Result.Failure<BranchDetails>(BranchErrors.NameTooLong);
        }

        slug = Normalize(slug)?.ToLowerInvariant();
        if (slug is not null && !IsValidSlug(slug))
        {
            return Result.Failure<BranchDetails>(BranchErrors.InvalidSlug);
        }

        phone = Normalize(phone);
        addressLine = Normalize(addressLine);
        cityName = Normalize(cityName);
        regionName = Normalize(regionName);
        postalCode = Normalize(postalCode);
        countryCode = Normalize(countryCode)?.ToUpperInvariant();
        timeZoneId = Normalize(timeZoneId);

        if (phone?.Length > MaxPhoneLength || addressLine?.Length > MaxAddressLength ||
            cityName?.Length > MaxLocationNameLength || regionName?.Length > MaxLocationNameLength ||
            postalCode?.Length > MaxPostalCodeLength || timeZoneId?.Length > MaxTimeZoneIdLength)
        {
            return Result.Failure<BranchDetails>(BranchErrors.DetailsTooLong);
        }

        if (countryCode is not null &&
            (countryCode.Length != 2 || !countryCode.All(char.IsAsciiLetter)))
        {
            return Result.Failure<BranchDetails>(BranchErrors.InvalidCountryCode);
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            return Result.Failure<BranchDetails>(BranchErrors.CoordinatesMustBeProvidedTogether);
        }

        if (latitude is < -90 or > 90)
        {
            return Result.Failure<BranchDetails>(BranchErrors.InvalidLatitude);
        }

        if (longitude is < -180 or > 180)
        {
            return Result.Failure<BranchDetails>(BranchErrors.InvalidLongitude);
        }

        return Result.Success(new BranchDetails(
            name, slug, phone, addressLine, cityName, regionName, postalCode,
            countryCode, latitude, longitude, timeZoneId));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidSlug(string slug) =>
        slug.Length is >= 3 and <= MaxSlugLength &&
        slug[0] != '-' && slug[^1] != '-' &&
        !slug.Contains("--", StringComparison.Ordinal) &&
        slug.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private bool Matches(BranchDetails details) =>
        Name == details.Name && Slug == details.Slug && Phone == details.Phone &&
        AddressLine == details.AddressLine && CityName == details.CityName &&
        RegionName == details.RegionName && PostalCode == details.PostalCode &&
        CountryCode == details.CountryCode && Latitude == details.Latitude &&
        Longitude == details.Longitude && TimeZoneId == details.TimeZoneId;

    private void Apply(BranchDetails details)
    {
        Name = details.Name;
        Slug = details.Slug;
        Phone = details.Phone;
        AddressLine = details.AddressLine;
        CityName = details.CityName;
        RegionName = details.RegionName;
        PostalCode = details.PostalCode;
        CountryCode = details.CountryCode;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        TimeZoneId = details.TimeZoneId;
    }

    private sealed record BranchDetails(
        string Name,
        string? Slug,
        string? Phone,
        string? AddressLine,
        string? CityName,
        string? RegionName,
        string? PostalCode,
        string? CountryCode,
        decimal? Latitude,
        decimal? Longitude,
        string? TimeZoneId);
}
