using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Payments.Application.Abstractions;
namespace RestaurantMenu.Api.Integrations.Payments;
public sealed class PayableOrderProvider(IDiningSessionRepository sessions,IDiningSessionTokenGenerator tokens,
    IOrderReadService orders,TimeProvider timeProvider):IPayableOrderProvider
{
 public async Task<PayableOrderSnapshot?>GetAsync(string diningSessionToken,Guid orderId,long tipAmountMinor,CancellationToken cancellationToken)
 {
  if(string.IsNullOrWhiteSpace(diningSessionToken)||tipAmountMinor<0)return null;
  var scope=await sessions.ResolveAsync(tokens.Hash(diningSessionToken),timeProvider.GetUtcNow(),cancellationToken);if(scope is null)return null;
  var order=await orders.GetGuestOrderAsync(scope.RestaurantId,scope.BranchId,scope.SessionId,new OrderId(orderId),cancellationToken);
  if(order is null || order.Status is "Cancelled" or "Rejected" or "Completed")return null;
  var exponent=CurrencyExponent(order.Currency);var factor=(decimal)Math.Pow(10,exponent);
  var subtotal=ToMinor(order.SubtotalAmount,factor);var tax=ToMinor(order.TaxAmount,factor);
  return new(order.OrderId,scope.RestaurantId,scope.BranchId,scope.SessionId,subtotal,0,tax,0,order.Currency);
 }
 private static long ToMinor(decimal amount,decimal factor)=>checked((long)decimal.Round(amount*factor,0,MidpointRounding.AwayFromZero));
 private static int CurrencyExponent(string currency)=>currency.ToUpperInvariant() switch
 {"BIF" or "CLP" or "DJF" or "GNF" or "JPY" or "KMF" or "KRW" or "MGA" or "PYG" or "RWF" or "UGX" or "VND" or "VUV" or "XAF" or "XOF" or "XPF"=>0,"BHD" or "JOD" or "KWD" or "OMR" or "TND"=>3,_=>2};
}
