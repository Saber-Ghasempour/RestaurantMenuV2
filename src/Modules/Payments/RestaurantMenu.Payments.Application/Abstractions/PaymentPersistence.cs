using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Domain.Profiles;

namespace RestaurantMenu.Payments.Application.Abstractions;

public interface IPaymentRepository
{
    void Add(Payment payment);
    Task<Payment?> GetAsync(PaymentId paymentId,Guid restaurantId,CancellationToken cancellationToken);
    Task<Payment?> GetByOrderAsync(Guid orderId,CancellationToken cancellationToken);
    Task<Payment?> GetByProviderIntentAsync(string providerIntentId,CancellationToken cancellationToken);
}
public interface IPaymentProfileRepository
{
    void Add(RestaurantPaymentProfile profile);
    Task<RestaurantPaymentProfile?> GetAsync(Guid restaurantId,CancellationToken cancellationToken);
}
public interface IPaymentsUnitOfWork
{ Task<int> SaveChangesAsync(CancellationToken cancellationToken=default); }

public sealed class DuplicatePaymentException(string message,Exception inner):Exception(message,inner);
