using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace RestaurantMenu.Payments.Infrastructure.Database;
public sealed class PaymentsDbContextFactory:IDesignTimeDbContextFactory<PaymentsDbContext>
{
 public PaymentsDbContext CreateDbContext(string[] args){var cs=Environment.GetEnvironmentVariable("ConnectionStrings__Payments")??throw new InvalidOperationException("Environment variable 'ConnectionStrings__Payments' is required.");return new(new DbContextOptionsBuilder<PaymentsDbContext>().UseNpgsql(cs,n=>n.MigrationsHistoryTable("__ef_migrations_history","payments")).Options);}
}
