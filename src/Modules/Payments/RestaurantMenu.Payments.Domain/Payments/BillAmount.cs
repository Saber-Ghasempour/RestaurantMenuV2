namespace RestaurantMenu.Payments.Domain.Payments;

public sealed record BillAmount(
    long SubtotalAmountMinor,
    long DiscountAmountMinor,
    long TaxAmountMinor,
    long TipAmountMinor,
    long OtherFeeAmountMinor,
    string Currency);
