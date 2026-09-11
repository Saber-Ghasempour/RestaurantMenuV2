using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Domain.Profiles;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Payments.Application.Payments;

public sealed record StartOnboardingCommand(Guid RestaurantId,string Country,string Currency,
    Uri RefreshUrl,Uri ReturnUrl):ICommand<Result<OnboardingResponse>>;
public sealed record OnboardingResponse(string AccountId,Uri Url);
public sealed record EnablePaymentsCommand(Guid RestaurantId):ICommand<Result<bool>>;
public sealed record CreatePaymentCommand(string DiningSessionToken,Guid OrderId,long TipAmountMinor)
    :ICommand<Result<PaymentResponse>>;
public sealed record PaymentResponse(Guid PaymentId,string ClientSecret,long GrossAmountMinor,
    long PlatformFeeAmountMinor,string Currency,string Status);
public sealed record ProcessWebhookCommand(string Payload,string SignatureHeader)
    :ICommand<Result<bool>>;
public sealed record RefundPaymentCommand(Guid RestaurantId,Guid PaymentId,long AmountMinor)
    :ICommand<Result<string>>;

public static class PaymentApplicationErrors
{
    public static readonly ErrorDetail OrderNotPayable=ErrorDetail.NotFound("Payments.OrderNotPayable","The order is not payable.");
    public static readonly ErrorDetail Duplicate=ErrorDetail.Conflict("Payments.AlreadyExists","A payment already exists for this order.");
    public static readonly ErrorDetail WebhookInvalid=ErrorDetail.Validation("Payments.InvalidWebhook","The payment webhook is invalid.");
    public static readonly ErrorDetail PaymentNotFound=ErrorDetail.NotFound("Payments.NotFound","The payment was not found.");
}

public sealed class RefundPaymentCommandHandler(IPaymentRepository payments,
    IPaymentProvider provider):ICommandHandler<RefundPaymentCommand,Result<string>>
{
    public async Task<Result<string>> Handle(RefundPaymentCommand command,CancellationToken cancellationToken)
    {
        var payment=await payments.GetAsync(new PaymentId(command.PaymentId),command.RestaurantId,cancellationToken);
        if(payment is null)return Result.Failure<string>(PaymentApplicationErrors.PaymentNotFound);
        if(payment.ProviderPaymentIntentId is null ||
            payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded) ||
            command.AmountMinor<=0 || command.AmountMinor>payment.GrossAmountMinor-payment.RefundedAmountMinor)
            return Result.Failure<string>(PaymentErrors.InvalidRefund);
        var key=$"refund:{payment.Id.Value:N}:{payment.Version}:{command.AmountMinor}";
        var refund=await provider.CreateRefundAsync(payment.ConnectedAccountId,
            payment.ProviderPaymentIntentId,command.AmountMinor,key,cancellationToken);
        return Result.Success(refund.Id);
    }
}

public sealed class StartOnboardingCommandHandler(IPaymentProfileRepository profiles,
    IPaymentProvider provider,IPaymentsUnitOfWork unitOfWork,TimeProvider timeProvider)
    :ICommandHandler<StartOnboardingCommand,Result<OnboardingResponse>>
{
    public async Task<Result<OnboardingResponse>> Handle(StartOnboardingCommand command,CancellationToken cancellationToken)
    {
        var country=command.Country?.Trim().ToUpperInvariant();
        var currency=command.Currency?.Trim().ToUpperInvariant();
        var refreshUrl=command.RefreshUrl;
        var returnUrl=command.ReturnUrl;
        var secureUrls=refreshUrl is not null&&returnUrl is not null&&
            refreshUrl.IsAbsoluteUri&&returnUrl.IsAbsoluteUri&&
            refreshUrl.Scheme==Uri.UriSchemeHttps&&returnUrl.Scheme==Uri.UriSchemeHttps&&
            string.Equals(refreshUrl.GetLeftPart(UriPartial.Authority),
                returnUrl.GetLeftPart(UriPartial.Authority),StringComparison.OrdinalIgnoreCase);
        if(country is null||country.Length!=2||!country.All(char.IsAsciiLetter)||
            currency is null||currency.Length!=3||!currency.All(char.IsAsciiLetter)||!secureUrls)
            return Result.Failure<OnboardingResponse>(ProfileErrors.InvalidProfile);
        var existing=await profiles.GetAsync(command.RestaurantId,cancellationToken);
        var accountId=existing?.StripeAccountId;
        if(existing is null)
        {
            ProviderAccount account;
            try{account=await provider.CreateConnectedAccountAsync(country,currency,
                command.RestaurantId.ToString("N"),cancellationToken);}catch(Exception){return Result.Failure<OnboardingResponse>(ProfileErrors.InvalidProfile);}
            var created=RestaurantPaymentProfile.Create(command.RestaurantId,account.AccountId,
                country,currency,300,timeProvider.GetUtcNow());
            if(created.IsFailure)return Result.Failure<OnboardingResponse>(created.Error);
            profiles.Add(created.Value); await unitOfWork.SaveChangesAsync(cancellationToken); accountId=account.AccountId;
        }
        var link=await provider.CreateOnboardingLinkAsync(accountId!,refreshUrl!,returnUrl!,cancellationToken);
        return Result.Success(new OnboardingResponse(accountId!,link.Url));
    }
}

public sealed class EnablePaymentsCommandHandler(IPaymentProfileRepository profiles,
    IPaymentProvider provider,IPaymentsUnitOfWork unitOfWork)
    :ICommandHandler<EnablePaymentsCommand,Result<bool>>
{
    public async Task<Result<bool>> Handle(EnablePaymentsCommand command,CancellationToken cancellationToken)
    {
        var profile=await profiles.GetAsync(command.RestaurantId,cancellationToken);
        if(profile is null)return Result.Failure<bool>(ProfileErrors.NotFound);
        if(!await provider.IsAccountReadyAsync(profile.StripeAccountId,cancellationToken))return Result.Failure<bool>(ProfileErrors.NotReady);
        profile.Enable();await unitOfWork.SaveChangesAsync(cancellationToken);return Result.Success(true);
    }
}

public sealed class CreatePaymentCommandHandler(IPayableOrderProvider orders,
    IPaymentProfileRepository profiles,IPaymentRepository payments,IPaymentProvider provider,
    IPaymentsUnitOfWork unitOfWork,TimeProvider timeProvider)
    :ICommandHandler<CreatePaymentCommand,Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand command,CancellationToken cancellationToken)
    {
        if(command.TipAmountMinor<0)return Result.Failure<PaymentResponse>(PaymentErrors.InvalidBillAmount);
        var order=await orders.GetAsync(command.DiningSessionToken,command.OrderId,command.TipAmountMinor,cancellationToken);
        if(order is null)return Result.Failure<PaymentResponse>(PaymentApplicationErrors.OrderNotPayable);
        if(await payments.GetByOrderAsync(order.OrderId,cancellationToken) is not null)return Result.Failure<PaymentResponse>(PaymentApplicationErrors.Duplicate);
        var profile=await profiles.GetAsync(order.RestaurantId,cancellationToken);
        if(profile is null || !profile.IsEnabled || profile.Currency!=order.Currency)
            return Result.Failure<PaymentResponse>(ProfileErrors.NotReady);
        var created=Payment.Create(PaymentId.New(),order.RestaurantId,order.BranchId,order.OrderId,
            order.DiningSessionId,profile.StripeAccountId,new BillAmount(order.SubtotalAmountMinor,
                order.DiscountAmountMinor,order.TaxAmountMinor,command.TipAmountMinor,
                order.OtherFeeAmountMinor,order.Currency),profile.CommissionRateBasisPoints,timeProvider.GetUtcNow());
        if(created.IsFailure)return Result.Failure<PaymentResponse>(created.Error);
        var payment=created.Value;
        var intent=await provider.CreatePaymentIntentAsync(profile.StripeAccountId,payment.GrossAmountMinor,
            payment.Currency,payment.PlatformFeeAmountMinor,order.OrderId.ToString("N"),cancellationToken);
        var attached=payment.AttachProviderIntent(intent.Id,"created:"+intent.Id,timeProvider.GetUtcNow());
        if(attached.IsFailure)return Result.Failure<PaymentResponse>(attached.Error);
        payments.Add(payment);await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new PaymentResponse(payment.Id.Value,intent.ClientSecret,payment.GrossAmountMinor,
            payment.PlatformFeeAmountMinor,payment.Currency,payment.Status.ToString()));
    }
}

public sealed class ProcessWebhookCommandHandler(IPaymentProvider provider,IPaymentRepository payments,
    IPaymentsUnitOfWork unitOfWork,TimeProvider timeProvider)
    :ICommandHandler<ProcessWebhookCommand,Result<bool>>
{
    public async Task<Result<bool>> Handle(ProcessWebhookCommand command,CancellationToken cancellationToken)
    {
        VerifiedPaymentEvent value;
        try{value=provider.VerifyWebhook(command.Payload,command.SignatureHeader,timeProvider.GetUtcNow());}
        catch(Exception){return Result.Failure<bool>(PaymentApplicationErrors.WebhookInvalid);}
        var payment=await payments.GetByProviderIntentAsync(value.PaymentIntentId,cancellationToken);
        if(payment is null)return Result.Success(true);
        Result<Payment> result=value.Type switch
        {
            "payment_intent.succeeded"=>payment.MarkSucceeded(value.EventId,value.OccurredAtUtc),
            "refund.created" or "refund.updated" when value.RefundAmountMinor.HasValue=>payment.RecordRefund(value.EventId,value.RefundAmountMinor.Value,value.OccurredAtUtc),
            _=>Result.Success(payment)
        };
        if(result.IsFailure)return Result.Failure<bool>(result.Error);
        await unitOfWork.SaveChangesAsync(cancellationToken);return Result.Success(true);
    }
}
