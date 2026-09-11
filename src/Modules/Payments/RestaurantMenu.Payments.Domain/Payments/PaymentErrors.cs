using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Payments.Domain.Payments;

public static class PaymentErrors
{
    public static readonly ErrorDetail InvalidScope = ErrorDetail.Validation(
        "Payments.InvalidScope", "The payment scope is invalid.");
    public static readonly ErrorDetail InvalidConnectedAccount = ErrorDetail.Validation(
        "Payments.InvalidConnectedAccount", "The connected payment account is invalid.");
    public static readonly ErrorDetail InvalidBillAmount = ErrorDetail.Validation(
        "Payments.InvalidBillAmount", "The final bill components are invalid.");
    public static readonly ErrorDetail InvalidCurrency = ErrorDetail.Validation(
        "Payments.InvalidCurrency", "A three-letter ISO currency is required.");
    public static readonly ErrorDetail InvalidCommissionRate = ErrorDetail.Validation(
        "Payments.InvalidCommissionRate", "Commission must be between 1 and 10,000 basis points.");
    public static readonly ErrorDetail ProviderIntentAlreadyAttached = ErrorDetail.Conflict(
        "Payments.ProviderIntentAlreadyAttached", "A provider intent is already attached.");
    public static readonly ErrorDetail InvalidProviderReference = ErrorDetail.Validation(
        "Payments.InvalidProviderReference", "The provider reference is invalid.");
    public static readonly ErrorDetail InvalidTransition = ErrorDetail.Conflict(
        "Payments.InvalidTransition", "The requested payment transition is invalid.");
    public static readonly ErrorDetail InvalidRefund = ErrorDetail.Validation(
        "Payments.InvalidRefund", "The refund amount is invalid.");
}
