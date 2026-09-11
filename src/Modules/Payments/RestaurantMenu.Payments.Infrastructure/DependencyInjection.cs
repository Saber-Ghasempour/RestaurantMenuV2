using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Application.Payments;
using RestaurantMenu.Payments.Infrastructure.Database;
using RestaurantMenu.Payments.Infrastructure.Stripe;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Payments.Infrastructure;
public static class DependencyInjection
{
 public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services,string connectionString,StripeOptions options)
 {
  if(string.IsNullOrWhiteSpace(options.SecretKey)||string.IsNullOrWhiteSpace(options.WebhookSecret))throw new InvalidOperationException("Stripe credentials are required.");
  services.AddDbContext<PaymentsDbContext>(o=>o.UseNpgsql(connectionString,n=>n.MigrationsHistoryTable("__ef_migrations_history","payments")));
  services.AddScoped<PaymentRepository>();services.AddScoped<IPaymentRepository>(x=>x.GetRequiredService<PaymentRepository>());services.AddScoped<IPaymentProfileRepository>(x=>x.GetRequiredService<PaymentRepository>());services.AddScoped<IPaymentsUnitOfWork>(x=>x.GetRequiredService<PaymentsDbContext>());
  services.AddSingleton(options);services.AddHttpClient<IPaymentProvider,StripePaymentProvider>(c=>{c.BaseAddress=options.ApiBaseUri;c.Timeout=options.Timeout;});
  services.AddScoped<ICommandHandler<StartOnboardingCommand,Result<OnboardingResponse>>,StartOnboardingCommandHandler>();services.AddScoped<ICommandHandler<EnablePaymentsCommand,Result<bool>>,EnablePaymentsCommandHandler>();services.AddScoped<ICommandHandler<CreatePaymentCommand,Result<PaymentResponse>>,CreatePaymentCommandHandler>();services.AddScoped<ICommandHandler<RefundPaymentCommand,Result<string>>,RefundPaymentCommandHandler>();services.AddScoped<ICommandHandler<ProcessWebhookCommand,Result<bool>>,ProcessWebhookCommandHandler>();return services;
 }
}
