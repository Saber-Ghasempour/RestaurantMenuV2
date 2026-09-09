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
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategoryContent;
using RestaurantMenu.Catalog.Application.Categories.ChangeMenuCategoryPublication;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;
using RestaurantMenu.Catalog.Application.Items.DeleteMenuItem;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Items.ListMenuItems;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItemMetadata;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemPublication;
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
using RestaurantMenu.Catalog.Infrastructure.Variants;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Application.Variants.CreateMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.UpdateMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.ChangeMenuItemVariantAvailability;
using RestaurantMenu.Catalog.Application.Variants.SetDefaultMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.DeleteMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.GetMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.ListMenuItemVariants;
using RestaurantMenu.Catalog.Domain.Variants;
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
        services.AddScoped<IMenuItemVariantRepository,
            MenuItemVariantRepository>();
        services.AddScoped<IMenuItemVariantReadService,
            MenuItemVariantReadService>();
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
        services.AddScoped<IPublicMenuCacheInvalidator,
            PublicMenuCacheInvalidator>();
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
            ICommandHandler<UpdateMenuCategoryContentCommand, Result<long>>,
            UpdateMenuCategoryContentCommandHandler>();
        services.AddScoped<
            ICommandHandler<ChangeMenuCategoryPublicationCommand, Result<long>>,
            ChangeMenuCategoryPublicationCommandHandler>();
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
            ICommandHandler<UpdateMenuItemMetadataCommand, Result<long>>,
            UpdateMenuItemMetadataCommandHandler>();
        services.AddScoped<
            ICommandHandler<ChangeMenuItemPublicationCommand, Result<long>>,
            ChangeMenuItemPublicationCommandHandler>();
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
            ICommandHandler<CreateMenuItemVariantCommand, Result<MenuItemVariantId>>,
            CreateMenuItemVariantCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateMenuItemVariantCommand, Result<long>>,
            UpdateMenuItemVariantCommandHandler>();
        services.AddScoped<
            ICommandHandler<ChangeMenuItemVariantAvailabilityCommand, Result<long>>,
            ChangeMenuItemVariantAvailabilityCommandHandler>();
        services.AddScoped<
            ICommandHandler<SetDefaultMenuItemVariantCommand, Result<long>>,
            SetDefaultMenuItemVariantCommandHandler>();
        services.AddScoped<
            ICommandHandler<DeleteMenuItemVariantCommand, Result<MenuItemVariantId>>,
            DeleteMenuItemVariantCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetMenuItemVariantQuery, Result<MenuItemVariantResponse>>,
            GetMenuItemVariantQueryHandler>();
        services.AddScoped<
            IQueryHandler<ListMenuItemVariantsQuery,
                Result<IReadOnlyList<MenuItemVariantResponse>>>,
            ListMenuItemVariantsQueryHandler>();
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
