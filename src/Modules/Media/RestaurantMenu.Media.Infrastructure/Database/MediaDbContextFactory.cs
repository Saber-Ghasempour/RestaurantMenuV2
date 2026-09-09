using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantMenu.Media.Infrastructure.Database;

public sealed class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Media") ??
            "Host=localhost;Port=5432;Database=restaurantmenu;Username=postgres;Password=restaurantmenu_dev";
        return new MediaDbContext(new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__ef_migrations_history", "media")).Options);
    }
}
