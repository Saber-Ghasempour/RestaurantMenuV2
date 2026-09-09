using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantMenu.Restaurants.Infrastructure.Database;

public sealed class RestaurantsDbContextFactory
    : IDesignTimeDbContextFactory<RestaurantsDbContext>
{
    private const string ConnectionStringVariable =
        "ConnectionStrings__Restaurants";

    public RestaurantsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringVariable)
            ?? throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringVariable}' is required for design-time operations.");

        var options = new DbContextOptionsBuilder<RestaurantsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    "__ef_migrations_history", "restaurants"))
            .Options;

        return new RestaurantsDbContext(options);
    }
}
