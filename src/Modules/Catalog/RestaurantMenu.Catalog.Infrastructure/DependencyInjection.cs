using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;
using RestaurantMenu.Catalog.Application.Items.DeleteMenuItem;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Items.ListMenuItems;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.Catalog.Application.Publications.GetBranchCatalogConfiguration;
using RestaurantMenu.Catalog.Application.Publications.SetBranchCategoryPublications;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Caching;
using RestaurantMenu.Catalog.Infrastructure.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Catalog.Infrastructure.Items;
using RestaurantMenu.Catalog.Infrastructure.PublicMenus;
using RestaurantMenu.Catalog.Infrastructure.Publications;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString,
        TimeSpan publicMenuCacheTimeToLive)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            publicMenuCacheTimeToLive,
            TimeSpan.Zero);

        services.AddDbContext<CatalogDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        "catalog")));

        services.AddScoped<IMenuCategoryRepository,
            MenuCategoryRepository>();
        services.AddScoped<IMenuCategoryReadService,
            MenuCategoryReadService>();
        services.AddScoped<IMenuItemRepository,
            MenuItemRepository>();
        services.AddScoped<IMenuItemReadService,
            MenuItemReadService>();
        services.AddScoped<IPublicMenuReadService,
            PublicMenuReadService>();
        services.AddScoped<IBranchCategoryPublicationRepository,
            BranchCategoryPublicationRepository>();
        services.AddScoped<IBranchCatalogReadService,
            BranchCatalogReadService>();
        services.AddSingleton(
            new PublicBranchMenuCacheOptions(publicMenuCacheTimeToLive));
        services.AddSingleton<IPublicBranchMenuCache,
            PublicBranchMenuCache>();
        services.AddScoped<ICatalogUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<
            ICommandHandler<
                CreateMenuCategoryCommand,
                Result<MenuCategoryId>>,
            CreateMenuCategoryCommandHandler>();
        services.AddScoped<
            IQueryHandler<
                GetMenuCategoryQuery,
                Result<MenuCategoryResponse>>,
            GetMenuCategoryQueryHandler>();
        services.AddScoped<
            IQueryHandler<
                ListMenuCategoriesQuery,
                Result<IReadOnlyList<MenuCategoryResponse>>>,
            ListMenuCategoriesQueryHandler>();
        services.AddScoped<
            ICommandHandler<
                UpdateMenuCategoryCommand,
                Result<long>>,
            UpdateMenuCategoryCommandHandler>();
        services.AddScoped<
            ICommandHandler<
                DeleteMenuCategoryCommand,
                Result<MenuCategoryId>>,
            DeleteMenuCategoryCommandHandler>();
        services.AddScoped<
            ICommandHandler<
                CreateMenuItemCommand,
                Result<MenuItemId>>,
            CreateMenuItemCommandHandler>();
        services.AddScoped<
            IQueryHandler<
                GetMenuItemQuery,
                Result<MenuItemResponse>>,
            GetMenuItemQueryHandler>();
        services.AddScoped<
            IQueryHandler<
                ListMenuItemsQuery,
                Result<IReadOnlyList<MenuItemResponse>>>,
            ListMenuItemsQueryHandler>();
        services.AddScoped<
            ICommandHandler<
                UpdateMenuItemCommand,
                Result<long>>,
            UpdateMenuItemCommandHandler>();
        services.AddScoped<
            ICommandHandler<
                ChangeMenuItemAvailabilityCommand,
                Result<long>>,
            ChangeMenuItemAvailabilityCommandHandler>();
        services.AddScoped<
            ICommandHandler<
                DeleteMenuItemCommand,
                Result<MenuItemId>>,
            DeleteMenuItemCommandHandler>();
        services.AddScoped<
            IQueryHandler<
                GetPublicMenuQuery,
                Result<PublicMenuResponse>>,
            GetPublicMenuQueryHandler>();
        services.AddScoped<
            ICommandHandler<SetBranchCategoryPublicationsCommand, Result<bool>>,
            SetBranchCategoryPublicationsCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetBranchCatalogConfigurationQuery,
                Result<IReadOnlyList<BranchCategoryPublicationResponse>>>,
            GetBranchCatalogConfigurationQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetPublicBranchMenuQuery,
                Result<PublicBranchMenuResponse>>,
            GetPublicBranchMenuQueryHandler>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
