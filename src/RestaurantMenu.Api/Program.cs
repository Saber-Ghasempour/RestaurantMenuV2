using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;

using RestaurantMenu.Api.Authentication;
using RestaurantMenu.Api.Health;
using RestaurantMenu.Api.Infrastructure;
using RestaurantMenu.Api.Integrations.Catalog;
using RestaurantMenu.Api.Observability;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Infrastructure;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Catalog.Presentation.Categories;
using RestaurantMenu.Catalog.Presentation.Items;
using RestaurantMenu.Catalog.Presentation.Variants;
using RestaurantMenu.Catalog.Presentation.PublicMenus;
using RestaurantMenu.Catalog.Presentation.Publications;
using RestaurantMenu.Restaurants.Infrastructure;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Presentation.Restaurants;
using RestaurantMenu.Restaurants.Presentation.Branches;
using RestaurantMenu.Restaurants.Presentation.DiningTables;
using RestaurantMenu.Restaurants.Presentation.PublicMenuCodes;
using RestaurantMenu.Media.Infrastructure;
using RestaurantMenu.Media.Infrastructure.Database;
using RestaurantMenu.Media.Infrastructure.Storage;
using RestaurantMenu.Media.Presentation.Assets;
using RestaurantMenu.Api.Integrations.Media;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Presentation.DiningSessions;
using RestaurantMenu.Api.Integrations.Ordering;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(
    options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "O";
        options.UseUtcTimestamp = true;
    });

var restaurantsConnectionString = builder.Configuration.GetConnectionString(
    "Restaurants")
    ?? throw new InvalidOperationException(
        "Connection string 'Restaurants' is not configured.");

var catalogConnectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException(
        "Connection string 'Catalog' is not configured.");
var mediaConnectionString = builder.Configuration.GetConnectionString("Media") ?? catalogConnectionString;
var orderingConnectionString = builder.Configuration.GetConnectionString("Ordering") ?? restaurantsConnectionString;
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "Connection string 'Redis' is not configured.");
var restaurantCacheTimeToLive = builder.Configuration.GetValue(
    "Caching:Restaurants:TimeToLive",
    TimeSpan.FromMinutes(5));
var redisConnectTimeoutMilliseconds = builder.Configuration.GetValue(
    "Caching:Redis:ConnectTimeoutMilliseconds",
    1000);
var redisOperationTimeoutMilliseconds = builder.Configuration.GetValue(
    "Caching:Redis:OperationTimeoutMilliseconds",
    1000);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(
    options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ??
                context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["correlationId"] =
                CorrelationIdMiddleware.GetCorrelationId(
                    context.HttpContext);
        });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddRateLimiter(options => options.AddPolicy("public-menu-code-resolution", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }))
    .AddPolicy("dining-session-start", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }))
    .AddPolicy("dining-session-use", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
builder.Services.AddRestaurantMenuAuthentication(
    builder.Configuration);
builder.Services.AddRestaurantMenuObservability(
    builder.Configuration);
builder.Services.AddRestaurantsInfrastructure(
    restaurantsConnectionString,
    redisConnectionString,
    restaurantCacheTimeToLive,
    redisConnectTimeoutMilliseconds,
    redisOperationTimeoutMilliseconds);
builder.Services.AddCatalogInfrastructure(
    catalogConnectionString,
    restaurantCacheTimeToLive);
builder.Services.AddMediaInfrastructure(mediaConnectionString, new ObjectStorageOptions(
    builder.Configuration["ObjectStorage:BucketName"] ?? "restaurant-menu-media",
    builder.Configuration["ObjectStorage:ServiceUrl"] ?? "http://localhost:9000",
    builder.Configuration["ObjectStorage:AccessKey"] ?? "minioadmin",
    builder.Configuration["ObjectStorage:SecretKey"] ?? "minioadmin",
    PublicServiceUrl: builder.Configuration["ObjectStorage:PublicServiceUrl"]));
builder.Services.AddOrderingInfrastructure(orderingConnectionString,
    builder.Configuration.GetValue("DiningSessions:Lifetime", TimeSpan.FromHours(2)));
builder.Services.AddScoped<IPublicCodeResolver, DiningSessionPublicCodeResolver>();
builder.Services.AddScoped<MediaAssetIntegrationService>();
builder.Services.AddScoped<RestaurantMenu.Restaurants.Application.Abstractions.Media.IMediaAssetValidator>(sp => sp.GetRequiredService<MediaAssetIntegrationService>());
builder.Services.AddScoped<RestaurantMenu.Catalog.Application.Abstractions.Media.IMediaAssetValidator>(sp => sp.GetRequiredService<MediaAssetIntegrationService>());
builder.Services.AddScoped<IMediaReferenceChecker>(sp => sp.GetRequiredService<MediaAssetIntegrationService>());
builder.Services.AddScoped<IPublicMediaReferenceChecker>(sp => sp.GetRequiredService<MediaAssetIntegrationService>());
builder.Services.AddScoped<
    IRestaurantExistenceChecker,
    RestaurantExistenceChecker>();
builder.Services.AddScoped<
    IRestaurantPublicProfileProvider,
    RestaurantPublicProfileProvider>();
builder.Services.AddScoped<IBranchExistenceChecker, BranchExistenceChecker>();
builder.Services.AddScoped<IBranchPublicProfileProvider, BranchPublicProfileProvider>();
builder.Services.AddScoped<
    RestaurantMenu.Restaurants.Application.Abstractions.Caching.IRestaurantPublicMenuInvalidator,
    RestaurantPublicMenuInvalidator>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddDbContextCheck<RestaurantsDbContext>(
        "restaurants-database",
        tags: ["ready"])
    .AddDbContextCheck<CatalogDbContext>(
        "catalog-database",
        tags: ["ready"])
    .AddDbContextCheck<MediaDbContext>("media-database", tags: ["ready"])
    .AddDbContextCheck<OrderingDbContext>("ordering-database", tags: ["ready"])
    .AddCheck<RedisHealthCheck>(
        "redis",
        tags: ["ready"]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await app.ApplyDatabaseMigrationsAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .AllowAnonymous();
}

if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapRestaurantsEndpoints();
app.MapBranchesEndpoints();
app.MapDiningTablesEndpoints();
app.MapPublicMenuCodesEndpoints();
app.MapMenuCategoryEndpoints();
app.MapMenuItemEndpoints();
app.MapMenuItemVariantEndpoints();
app.MapPublicMenuEndpoints();
app.MapBranchCategoryPublicationEndpoints();
app.MapMediaAssetEndpoints();
app.MapPublicMenuSlugEndpoints();
app.MapPublicMenuCodeMenuEndpoints();
app.MapDiningSessionEndpoints();
app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = registration =>
                registration.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        })
    .AllowAnonymous();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration =>
                registration.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        })
    .AllowAnonymous();

app.Run();

public partial class Program
{
}
