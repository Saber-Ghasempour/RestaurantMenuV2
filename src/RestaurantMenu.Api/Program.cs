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
using RestaurantMenu.Catalog.Presentation.PublicMenus;
using RestaurantMenu.Catalog.Presentation.Publications;
using RestaurantMenu.Restaurants.Infrastructure;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Presentation.Restaurants;
using RestaurantMenu.Restaurants.Presentation.Branches;
using RestaurantMenu.Restaurants.Presentation.DiningTables;
using RestaurantMenu.Restaurants.Presentation.PublicMenuCodes;

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
        })));
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
app.MapPublicMenuEndpoints();
app.MapBranchCategoryPublicationEndpoints();
app.MapPublicMenuSlugEndpoints();
app.MapPublicMenuCodeMenuEndpoints();
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
