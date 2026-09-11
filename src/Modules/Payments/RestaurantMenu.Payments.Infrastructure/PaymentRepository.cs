using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Domain.Profiles;
using RestaurantMenu.Payments.Infrastructure.Database;
namespace RestaurantMenu.Payments.Infrastructure;
public sealed class PaymentRepository(PaymentsDbContext db):IPaymentRepository,IPaymentProfileRepository
{
 public void Add(Payment payment)=>db.Payments.Add(payment);
 public Task<Payment?> GetAsync(PaymentId paymentId,Guid restaurantId,CancellationToken cancellationToken)=>db.Payments.Include(x=>x.Events).SingleOrDefaultAsync(x=>x.Id==paymentId&&x.RestaurantId==restaurantId,cancellationToken);
 void IPaymentProfileRepository.Add(RestaurantPaymentProfile profile)=>db.Profiles.Add(profile);
 public Task<Payment?> GetByOrderAsync(Guid orderId,CancellationToken cancellationToken)=>db.Payments.Include(x=>x.Events).SingleOrDefaultAsync(x=>x.OrderId==orderId,cancellationToken);
 public Task<Payment?> GetByProviderIntentAsync(string providerIntentId,CancellationToken cancellationToken)=>db.Payments.Include(x=>x.Events).SingleOrDefaultAsync(x=>x.ProviderPaymentIntentId==providerIntentId,cancellationToken);
 public Task<RestaurantPaymentProfile?> GetAsync(Guid restaurantId,CancellationToken cancellationToken)=>db.Profiles.SingleOrDefaultAsync(x=>x.RestaurantId==restaurantId,cancellationToken);
}
