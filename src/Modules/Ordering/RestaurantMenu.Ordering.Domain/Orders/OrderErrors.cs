using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Domain.Orders;

public static class OrderErrors
{
    public static readonly ErrorDetail InvalidScope = ErrorDetail.Validation("Ordering.InvalidOrderScope", "The order scope is invalid.");
    public static readonly ErrorDetail InvalidPublicNumber = ErrorDetail.Validation("Ordering.InvalidPublicNumber", "The public order number is invalid.");
    public static readonly ErrorDetail LinesRequired = ErrorDetail.Validation("Ordering.OrderLinesRequired", "At least one order line is required.");
    public static readonly ErrorDetail TooManyLines = ErrorDetail.Validation("Ordering.TooManyOrderLines", $"An order cannot contain more than {Order.MaxLineCount} lines.");
    public static readonly ErrorDetail InvalidQuantity = ErrorDetail.Validation("Ordering.InvalidOrderQuantity", $"Quantity must be between 1 and {OrderLine.MaxQuantity}.");
    public static readonly ErrorDetail InvalidPrice = ErrorDetail.Validation("Ordering.InvalidOrderPrice", "A trusted line price must be non-negative with at most two decimal places.");
    public static readonly ErrorDetail InvalidCurrency = ErrorDetail.Validation("Ordering.InvalidOrderCurrency", "Currency must be a three-letter ISO code.");
    public static readonly ErrorDetail InvalidTax = ErrorDetail.Validation("Ordering.InvalidTax", "The item tax rate or behavior is invalid.");
    public static readonly ErrorDetail MixedCurrencies = ErrorDetail.Validation("Ordering.MixedOrderCurrencies", "All order lines must use the same currency.");
    public static readonly ErrorDetail ItemNameRequired = ErrorDetail.Validation("Ordering.OrderItemNameRequired", "An item snapshot name is required.");
    public static readonly ErrorDetail ItemNameTooLong = ErrorDetail.Validation("Ordering.OrderItemNameTooLong", "An item snapshot name is too long.");
    public static readonly ErrorDetail VariantNameTooLong = ErrorDetail.Validation("Ordering.OrderVariantNameTooLong", "A variant snapshot name is too long.");
    public static readonly ErrorDetail LineNoteTooLong = ErrorDetail.Validation("Ordering.OrderLineNoteTooLong", "An order line note is too long.");
    public static readonly ErrorDetail CustomerNoteTooLong = ErrorDetail.Validation("Ordering.CustomerNoteTooLong", "The customer note is too long.");
    public static readonly ErrorDetail TableNameRequired = ErrorDetail.Validation("Ordering.TableNameRequired", "A trusted table display name is required.");
    public static readonly ErrorDetail StaffSubjectRequired = ErrorDetail.Validation("Ordering.StaffSubjectRequired", "A staff subject is required.");
    public static readonly ErrorDetail TransitionReasonRequired = ErrorDetail.Validation("Ordering.TransitionReasonRequired", "A reason is required for this transition.");
    public static readonly ErrorDetail TransitionReasonTooLong = ErrorDetail.Validation("Ordering.TransitionReasonTooLong", $"A transition reason cannot exceed {OrderStatusHistory.MaxReasonLength} characters.");
    public static ErrorDetail InvalidTransition(OrderStatus from, OrderStatus to) =>
        ErrorDetail.Conflict("Ordering.InvalidOrderTransition", $"An order cannot transition from {from} to {to}.");
}
