using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Publications;

namespace RestaurantMenu.Catalog.Infrastructure.Database;

public sealed class CatalogDbContext
    : DbContext,
      ICatalogUnitOfWork
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options)
        : base(
            options ??
            throw new ArgumentNullException(nameof(options)))
    {
    }

    public DbSet<MenuCategory> MenuCategories =>
        Set<MenuCategory>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<BranchCategoryPublication> BranchCategoryPublications =>
        Set<BranchCategoryPublication>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException(
                "A concurrent Catalog database update was detected.",
                exception);
        }
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CatalogDbContext).Assembly);
    }
}
