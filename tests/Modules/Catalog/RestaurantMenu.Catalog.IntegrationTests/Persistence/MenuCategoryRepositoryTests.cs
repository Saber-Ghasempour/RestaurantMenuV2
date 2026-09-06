using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Infrastructure.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class MenuCategoryRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task AddShouldPersistCategoryHierarchy()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var parentResult = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            "Main Courses",
            1,
            DateTimeOffset.UtcNow);

        Assert.True(parentResult.IsSuccess);

        var childResult = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            parentResult.Value.Id,
            "Pasta",
            2,
            DateTimeOffset.UtcNow);

        Assert.True(childResult.IsSuccess);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new MenuCategoryRepository(context);
            repository.Add(parentResult.Value);
            repository.Add(childResult.Value);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new CatalogDbContext(options);
        var repositoryForVerification =
            new MenuCategoryRepository(verificationContext);

        var persistedChild =
            await repositoryForVerification.GetByIdAsync(
                childResult.Value.Id,
                CancellationToken.None);

        Assert.NotNull(persistedChild);
        Assert.Equal(restaurantId, persistedChild.RestaurantId);
        Assert.Equal(parentResult.Value.Id, persistedChild.ParentId);
        Assert.Equal("Pasta", persistedChild.Name);
        Assert.Equal(2, persistedChild.DisplayOrder);
        Assert.Equal(1, persistedChild.Version);
    }
}
