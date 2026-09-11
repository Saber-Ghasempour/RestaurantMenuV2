using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Payments.Application.Payments;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Payments.Presentation;
public static class PaymentEndpoints
{
 public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder e)
 {
  var management=e.MapGroup("/api/restaurants/{restaurantId:guid}/payments").WithTags("Payments");
  management.MapPost("/onboarding",Onboard).RequireRestaurantAccess(Permissions.RestaurantsWrite);
  management.MapPost("/enable",Enable).RequireRestaurantAccess(Permissions.RestaurantsWrite);
  management.MapPost("/{paymentId:guid}/refunds",Refund).RequireRestaurantAccess(Permissions.RestaurantsWrite);
  e.MapPost("/api/public/orders/{orderId:guid}/payments",Create).WithTags("Payments").AllowAnonymous().RequireRateLimiting("dining-session-use");
  e.MapPost("/api/webhooks/stripe",Webhook).WithTags("Payments").AllowAnonymous()
   .WithMetadata(new RequestSizeLimitAttribute(1_048_576));return e;
 }
 private static async Task<IResult>Onboard(Guid restaurantId,OnboardingRequest request,ICommandHandler<StartOnboardingCommand,Result<OnboardingResponse>>h,CancellationToken ct){var r=await h.Handle(new(restaurantId,request.Country,request.Currency,request.RefreshUrl,request.ReturnUrl),ct);return r.IsFailure?r.Error.ToProblem():Results.Ok(r.Value);}
 private static async Task<IResult>Enable(Guid restaurantId,ICommandHandler<EnablePaymentsCommand,Result<bool>>h,CancellationToken ct){var r=await h.Handle(new(restaurantId),ct);return r.IsFailure?r.Error.ToProblem():Results.NoContent();}
 private static async Task<IResult>Refund(Guid restaurantId,Guid paymentId,RefundPaymentRequest request,ICommandHandler<RefundPaymentCommand,Result<string>>h,CancellationToken ct){var r=await h.Handle(new(restaurantId,paymentId,request.AmountMinor),ct);return r.IsFailure?r.Error.ToProblem():Results.Accepted(value:new RefundPaymentResponse(r.Value));}
 private static async Task<IResult>Create(Guid orderId,CreatePaymentRequest request,HttpContext context,ICommandHandler<CreatePaymentCommand,Result<PaymentResponse>>h,CancellationToken ct){var token=ReadDiningSessionToken(context.Request);if(token is null)return Results.Unauthorized();var r=await h.Handle(new(token,orderId,request.TipAmountMinor),ct);return r.IsFailure?r.Error.ToProblem():Results.Created($"/api/public/orders/{orderId}/payments/{r.Value.PaymentId}",r.Value);}
 private static async Task<IResult>Webhook(HttpRequest request,ICommandHandler<ProcessWebhookCommand,Result<bool>>h,CancellationToken ct){using var reader=new StreamReader(request.Body);var payload=await reader.ReadToEndAsync(ct);var signature=request.Headers["Stripe-Signature"].FirstOrDefault()??string.Empty;var r=await h.Handle(new(payload,signature),ct);return r.IsFailure?r.Error.ToProblem():Results.Ok();}
 private static string? ReadDiningSessionToken(HttpRequest request){var hasHeader=request.Headers.TryGetValue("X-Dining-Session",out StringValues values)&&values.Count==1&&!string.IsNullOrWhiteSpace(values[0]);var hasCookie=request.Cookies.TryGetValue("rm_dining_session",out var cookie)&&!string.IsNullOrWhiteSpace(cookie);if(hasHeader&&hasCookie&&!string.Equals(values[0],cookie,StringComparison.Ordinal))return null;return hasHeader?values[0]:hasCookie?cookie:null;}
}
public sealed record OnboardingRequest(string Country,string Currency,Uri RefreshUrl,Uri ReturnUrl);
public sealed record CreatePaymentRequest(long TipAmountMinor);
public sealed record RefundPaymentRequest(long AmountMinor);
public sealed record RefundPaymentResponse(string RefundId);
