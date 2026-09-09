using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Infrastructure.Database;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options), IMediaUnitOfWork
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception) { throw new ConcurrencyException("A concurrent Media database update was detected.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("ux_media_assets_restaurant_sha256_active", StringComparison.Ordinal) == true)
        { throw new DuplicateMediaChecksumException(exception); }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
    }
}
