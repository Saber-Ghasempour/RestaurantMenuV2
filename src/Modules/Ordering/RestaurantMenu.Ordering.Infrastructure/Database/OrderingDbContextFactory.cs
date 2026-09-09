using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantMenu.Ordering.Infrastructure.Database;
public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Ordering")
            ?? throw new InvalidOperationException("Environment variable 'ConnectionStrings__Ordering' is required for design-time operations.");
        var options = new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "ordering")).Options;
        return new OrderingDbContext(options);
    }
}
