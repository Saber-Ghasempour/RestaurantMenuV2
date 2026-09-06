using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Infrastructure.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

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
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
