using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantMenu.Catalog.Infrastructure.Database;

public sealed class CatalogDbContextFactory
    : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string ConnectionStringVariable =
        "ConnectionStrings__Catalog";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionStringVariable)
            ?? throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringVariable}' is required for design-time operations.");

        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;

        return new CatalogDbContext(options);
    }
}
