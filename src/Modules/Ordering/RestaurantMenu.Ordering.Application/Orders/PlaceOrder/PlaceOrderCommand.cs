using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.PlaceOrder;

public sealed record PlaceOrderLine(Guid MenuItemId, Guid? VariantId, int Quantity, string? Note);
public sealed record PlaceOrderCommand(string DiningSessionToken, string IdempotencyKey,
    string? CustomerNote, IReadOnlyList<PlaceOrderLine>? Lines) : ICommand<Result<PlaceOrderResponse>>;
public sealed record PlaceOrderResponse(Guid OrderId, string PublicNumber, string Status,
    string Currency, decimal SubtotalAmount, decimal TaxAmount, decimal TotalAmount, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<PlacedOrderLineResponse> Lines, bool IsReplay);
public sealed record PlacedOrderLineResponse(Guid Id, Guid MenuItemId, Guid? VariantId,
    string ItemName, string? VariantName, decimal UnitPriceAmount, string Currency,
    int Quantity, decimal NetAmount, decimal TaxAmount, decimal LineTotalAmount,
    int TaxRateBasisPoints, string TaxBehavior, string? Note);
