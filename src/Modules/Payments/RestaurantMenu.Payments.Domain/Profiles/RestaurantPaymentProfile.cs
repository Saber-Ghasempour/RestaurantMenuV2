using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;
using RestaurantMenu.Payments.Domain.Payments;

namespace RestaurantMenu.Payments.Domain.Profiles;

public sealed class RestaurantPaymentProfile : AggregateRoot<Guid>
{
    private RestaurantPaymentProfile() : base(Guid.Empty)
    { StripeAccountId = Country = Currency = string.Empty; }
    private RestaurantPaymentProfile(Guid id, Guid restaurantId, string stripeAccountId,
        string country, string currency, int commissionRateBasisPoints,
        DateTimeOffset createdAtUtc) : base(id)
    { RestaurantId=restaurantId; StripeAccountId=stripeAccountId; Country=country;
      Currency=currency; CommissionRateBasisPoints=commissionRateBasisPoints;
      CreatedAtUtc=createdAtUtc; }
    public Guid RestaurantId { get; }
    public string StripeAccountId { get; }
    public string Country { get; }
    public string Currency { get; }
    public int CommissionRateBasisPoints { get; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;

    public static Result<RestaurantPaymentProfile> Create(Guid restaurantId,
        string? stripeAccountId, string? country, string? currency, int basisPoints,
        DateTimeOffset now)
    {
        stripeAccountId=stripeAccountId?.Trim(); country=country?.Trim().ToUpperInvariant();
        currency=currency?.Trim().ToUpperInvariant();
        if(restaurantId==Guid.Empty || string.IsNullOrWhiteSpace(stripeAccountId) || stripeAccountId.Length>255)
            return Result.Failure<RestaurantPaymentProfile>(ProfileErrors.InvalidProfile);
        if(country is null || country.Length!=2 || !country.All(char.IsAsciiLetter) ||
           currency is null || currency.Length!=3 || !currency.All(char.IsAsciiLetter))
            return Result.Failure<RestaurantPaymentProfile>(ProfileErrors.InvalidProfile);
        if(basisPoints is < 1 or > 10_000)
            return Result.Failure<RestaurantPaymentProfile>(PaymentErrors.InvalidCommissionRate);
        return Result.Success(new RestaurantPaymentProfile(Guid.CreateVersion7(),restaurantId,
            stripeAccountId,country,currency,basisPoints,now));
    }
    public void Enable(){if(!IsEnabled){IsEnabled=true;Version++;}}
}

public static class ProfileErrors
{
    public static readonly ErrorDetail InvalidProfile=ErrorDetail.Validation("Payments.InvalidProfile","The payment profile is invalid.");
    public static readonly ErrorDetail NotFound=ErrorDetail.NotFound("Payments.ProfileNotFound","The restaurant payment profile was not found.");
    public static readonly ErrorDetail NotReady=ErrorDetail.Conflict("Payments.ProfileNotReady","The restaurant payment profile is not ready to accept payments.");
}
