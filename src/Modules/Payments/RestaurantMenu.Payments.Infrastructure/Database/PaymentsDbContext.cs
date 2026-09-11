using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Domain.Profiles;

namespace RestaurantMenu.Payments.Infrastructure.Database;
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options):DbContext(options),IPaymentsUnitOfWork
{
    public DbSet<Payment> Payments=>Set<Payment>();
    public DbSet<RestaurantPaymentProfile> Profiles=>Set<RestaurantPaymentProfile>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {modelBuilder.HasDefaultSchema("payments");modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);}
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken=default)
    {try{return await base.SaveChangesAsync(cancellationToken);}catch(DbUpdateException ex) when(ex.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation}){throw new DuplicatePaymentException("Payment uniqueness conflict.",ex);}}
}
