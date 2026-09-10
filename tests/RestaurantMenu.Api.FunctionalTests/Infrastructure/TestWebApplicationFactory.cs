using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Media.Infrastructure.Database;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class TestWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private const string RestaurantsConnectionStringVariable =
        "ConnectionStrings__Restaurants";

    private const string CatalogConnectionStringVariable =
        "ConnectionStrings__Catalog";

    private const string RedisConnectionStringVariable =
        "ConnectionStrings__Redis";
    private const string RabbitMqConnectionStringVariable =
        "ConnectionStrings__RabbitMq";

    private static readonly object EnvironmentVariableLock =
        new();

    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .Build();

    private readonly RedisContainer _redis =
        new RedisBuilder("redis:8.10.1-alpine")
            .Build();
    private readonly RabbitMqContainer _rabbitMq =
        new RabbitMqBuilder("rabbitmq:4.3.5-alpine").Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _rabbitMq.StartAsync());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _rabbitMq.DisposeAsync().AsTask());

        Dispose();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureTestServices(
            services =>
            {
                services
                    .AddAuthentication(
                        TestAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<
                        Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                        TestAuthenticationHandler>(
                            TestAuthenticationHandler.AuthenticationScheme,
                            _ => { });
            });
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        lock (EnvironmentVariableLock)
        {
            var previousRestaurantsConnectionString =
                Environment.GetEnvironmentVariable(
                    RestaurantsConnectionStringVariable);
            var previousCatalogConnectionString =
                Environment.GetEnvironmentVariable(
                    CatalogConnectionStringVariable);
            var previousRedisConnectionString =
                Environment.GetEnvironmentVariable(
                    RedisConnectionStringVariable);
            var previousRabbitMqConnectionString = Environment.GetEnvironmentVariable(
                RabbitMqConnectionStringVariable);
            var connectionString =
                _postgres.GetConnectionString();

            Environment.SetEnvironmentVariable(
                RestaurantsConnectionStringVariable,
                connectionString);
            Environment.SetEnvironmentVariable(
                CatalogConnectionStringVariable,
                connectionString);
            Environment.SetEnvironmentVariable(
                RedisConnectionStringVariable,
                _redis.GetConnectionString());
            Environment.SetEnvironmentVariable(RabbitMqConnectionStringVariable,
                _rabbitMq.GetConnectionString());

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    RestaurantsConnectionStringVariable,
                    previousRestaurantsConnectionString);
                Environment.SetEnvironmentVariable(
                    CatalogConnectionStringVariable,
                    previousCatalogConnectionString);
                Environment.SetEnvironmentVariable(
                    RedisConnectionStringVariable,
                    previousRedisConnectionString);
                Environment.SetEnvironmentVariable(RabbitMqConnectionStringVariable,
                    previousRabbitMqConnectionString);
            }
        }
    }

    public async Task MigrateDatabaseAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task MigrateCatalogDatabaseAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task MigrateMediaDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task MigrateOrderingDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task<DiningSession?> FindDiningSessionAsync(Guid sessionId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        return await dbContext.DiningSessions.AsNoTracking()
            .SingleOrDefaultAsync(session => session.Id == new DiningSessionId(sessionId));
    }

    public async Task<Order?> FindOrderAsync(Guid orderId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        return await dbContext.Orders.AsNoTracking().Include(order => order.Lines)
            .Include(order => order.StatusHistory)
            .SingleOrDefaultAsync(order => order.Id == new OrderId(orderId));
    }

    public async Task<Order> SeedOrderAsync(Guid restaurantId, Guid branchId,
        OrderStatus status = OrderStatus.Placed)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var id = OrderId.New();
        var order = Order.Create(id, $"T-{Guid.NewGuid():N}"[..20], restaurantId, branchId,
            Guid.NewGuid(), "Table 1", Guid.NewGuid(), null,
            [new OrderLineSnapshot(Guid.NewGuid(), null, "Soup", null, 5m, "EUR", 1, null)],
            DateTimeOffset.UtcNow).Value;
        AdvanceOrder(order, status);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
    }

    public async Task SeedRestaurantMemberAsync(Guid restaurantId, string subject)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantsDbContext>();
        dbContext.RestaurantMemberships.Add(RestaurantMembership.Create(new RestaurantId(restaurantId),
            subject, RestaurantMembershipRole.Staff, DateTimeOffset.UtcNow).Value);
        await dbContext.SaveChangesAsync();
    }

    public async Task SeedBranchMembershipAsync(Guid restaurantId, Guid branchId, string subject,
        BranchMembershipRole role, BranchMembershipStatus status = BranchMembershipStatus.Active)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantsDbContext>();
        var membership = BranchMembership.Create(new RestaurantId(restaurantId), new BranchId(branchId),
            subject, role, DateTimeOffset.UtcNow, status).Value;
        dbContext.BranchMemberships.Add(membership);
        await dbContext.SaveChangesAsync();
    }

    private static void AdvanceOrder(Order order, OrderStatus status)
    {
        const string subject = "seed-staff";
        var now = DateTimeOffset.UtcNow;
        if (status == OrderStatus.Placed) return;
        if (status == OrderStatus.Rejected) { order.Reject(subject, "seed rejection", now); return; }
        if (status == OrderStatus.Cancelled) { order.Cancel(subject, "seed cancellation", now); return; }
        order.Accept(subject, now);
        if (status == OrderStatus.Accepted) return;
        order.StartPreparing(subject, now);
        if (status == OrderStatus.Preparing) return;
        order.MarkReady(subject, now);
        if (status == OrderStatus.Ready) return;
        order.MarkServed(subject, now);
        if (status == OrderStatus.Served) return;
        order.Complete(subject, now);
    }

    public async Task<int> CountOrdersAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OrderingDbContext>()
            .Orders.CountAsync();
    }

    public async Task<MediaAsset> SeedReadyMediaAssetAsync(Guid restaurantId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        const string hash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        var asset = MediaAsset.Initiate(MediaAssetId.New(), restaurantId,
            $"restaurants/{restaurantId:N}/{Guid.CreateVersion7():N}", "seed.png",
            "image/png", 68, hash, TestAuthenticationHandler.DefaultSubject, DateTimeOffset.UtcNow).Value;
        asset.Complete("image/png", 68, hash, 1, 1);
        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset;
    }

    public async Task<MenuCategory?> FindMenuCategoryAsync(
        Guid categoryId)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();
        var id = new MenuCategoryId(categoryId);

        return await dbContext.MenuCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id);
    }

    public async Task<MenuCategory?>
        FindMenuCategoryIncludingDeletedAsync(
            Guid categoryId)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();
        var id = new MenuCategoryId(categoryId);

        return await dbContext.MenuCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id);
    }

    public async Task<int> CountMenuCategoriesAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        return await dbContext.MenuCategories.CountAsync();
    }

    public async Task<MenuItem?> FindMenuItemAsync(Guid menuItemId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var id = new MenuItemId(menuItemId);

        return await dbContext.MenuItems
            .AsNoTracking()
            .SingleOrDefaultAsync(menuItem => menuItem.Id == id);
    }

    public async Task<MenuItem?> FindMenuItemIncludingDeletedAsync(
        Guid menuItemId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var id = new MenuItemId(menuItemId);

        return await dbContext.MenuItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(menuItem => menuItem.Id == id);
    }

    public async Task<MenuItemVariant?> FindDefaultMenuItemVariantAsync(
        Guid menuItemId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var id = new MenuItemId(menuItemId);
        return await dbContext.MenuItemVariants.AsNoTracking()
            .SingleOrDefaultAsync(variant =>
                variant.MenuItemId == id && variant.IsDefault);
    }

    public async Task<int> CountMenuItemsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await dbContext.MenuItems.CountAsync();
    }

    public async Task<MenuItem> SeedMenuItemAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        string name,
        decimal priceAmount,
        string currency,
        int displayOrder,
        bool isPublished = false)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var result = MenuItem.Create(
            MenuItemId.New(),
            restaurantId,
            categoryId,
            name,
            null,
            displayOrder,
            DateTimeOffset.UtcNow);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not seed menu item: {result.Error.Code}");
        }

        if (isPublished)
        {
            result.Value.ChangePublication(true);
        }

        var variant = MenuItemVariant.Create(
            MenuItemVariantId.New(), restaurantId, result.Value.Id,
            "Default", null, priceAmount, currency, 0, true,
            DateTimeOffset.UtcNow).Value;
        dbContext.MenuItems.Add(result.Value);
        dbContext.MenuItemVariants.Add(variant);
        await dbContext.SaveChangesAsync();
        return result.Value;
    }

    public async Task SetMenuItemAvailabilityAsync(
        MenuItemId menuItemId,
        bool isAvailable)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var menuItem = await dbContext.MenuItems.SingleAsync(
            item => item.Id == menuItemId);

        menuItem.ChangeAvailability(isAvailable);
        await dbContext.SaveChangesAsync();
    }

    public async Task SetMenuItemPublicationAsync(MenuItemId menuItemId, bool isPublished)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var menuItem = await dbContext.MenuItems.SingleAsync(item => item.Id == menuItemId);
        menuItem.ChangePublication(isPublished);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteMenuItemAsync(MenuItemId menuItemId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var menuItem = await dbContext.MenuItems.SingleAsync(
            item => item.Id == menuItemId);

        menuItem.Delete(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteMenuCategoryAsync(
        MenuCategoryId categoryId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var category = await dbContext.MenuCategories.SingleAsync(
            item => item.Id == categoryId);

        category.Delete(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    public async Task<MenuCategory> SeedMenuCategoryAsync(
        Guid restaurantId,
        string name,
        int displayOrder,
        MenuCategoryId? parentId = null,
        bool isPublished = false)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            parentId,
            name,
            displayOrder,
            DateTimeOffset.UtcNow);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not seed menu category: {result.Error.Code}");
        }

        if (isPublished)
        {
            result.Value.ChangePublication(true);
        }

        dbContext.MenuCategories.Add(result.Value);
        await dbContext.SaveChangesAsync();

        return result.Value;
    }

    public async Task<Restaurant?> FindRestaurantAsync(
        Guid restaurantId)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        var id = new RestaurantId(restaurantId);

        return await dbContext.Restaurants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                restaurant => restaurant.Id == id);
    }

    public async Task<Branch?> FindBranchAsync(Guid branchId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantsDbContext>();
        var id = new BranchId(branchId);
        return await dbContext.Branches.AsNoTracking()
            .SingleOrDefaultAsync(branch => branch.Id == id);
    }

    public async Task<Branch?> FindBranchIncludingDeletedAsync(Guid branchId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantsDbContext>();
        var id = new BranchId(branchId);
        return await dbContext.Branches.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(branch => branch.Id == id);
    }

    public async Task<Restaurant?> FindRestaurantIncludingDeletedAsync(
        Guid restaurantId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();
        var id = new RestaurantId(restaurantId);

        return await dbContext.Restaurants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                restaurant => restaurant.Id == id);
    }

    public async Task<int> CountRestaurantsAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        return await dbContext.Restaurants.CountAsync();
    }

    public async Task<RestaurantMembership?> FindRestaurantMembershipAsync(
        Guid restaurantId,
        string subject)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();
        var id = new RestaurantId(restaurantId);

        return await dbContext.RestaurantMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(
                membership =>
                    membership.RestaurantId == id &&
                    membership.Subject == subject);
    }

    public async Task<RestaurantResponse?> GetCachedRestaurantAsync(
        RestaurantId restaurantId)
    {
        await using var scope = Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<
            IRestaurantCache>();

        return await cache.GetAsync(
            restaurantId,
            CancellationToken.None);
    }

    public async Task<Restaurant> SeedRestaurantAsync(
        string name,
        DateTimeOffset createdAtUtc,
        string ownerSubject = TestAuthenticationHandler.DefaultSubject)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        var result = Restaurant.Create(
            RestaurantId.New(),
            name,
            createdAtUtc);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not seed restaurant: {result.Error.Code}");
        }

        dbContext.Restaurants.Add(result.Value);

        var membershipResult = RestaurantMembership.Create(
            result.Value.Id,
            ownerSubject,
            RestaurantMembershipRole.Owner,
            createdAtUtc);

        if (membershipResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not seed membership: {membershipResult.Error.Code}");
        }

        dbContext.RestaurantMemberships.Add(
            membershipResult.Value);

        await dbContext.SaveChangesAsync();

        return result.Value;
    }
}
