using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(
        decimal amount,
        string? currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(
                MenuItemErrors.NegativePrice);
        }

        if (decimal.Round(amount, 2) != amount)
        {
            return Result.Failure<Money>(
                MenuItemErrors.PricePrecisionExceeded);
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(
                MenuItemErrors.CurrencyRequired);
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 ||
            !normalizedCurrency.All(char.IsAsciiLetter))
        {
            return Result.Failure<Money>(
                MenuItemErrors.InvalidCurrency);
        }

        return Result.Success(
            new Money(amount, normalizedCurrency));
    }
}
