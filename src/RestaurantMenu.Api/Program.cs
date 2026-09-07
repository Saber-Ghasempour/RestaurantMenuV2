using System.Diagnostics;

using RestaurantMenu.Api.Infrastructure;
using RestaurantMenu.Api.Integrations.Catalog;
using RestaurantMenu.Api.Observability;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Infrastructure;
using RestaurantMenu.Catalog.Presentation.Categories;
using RestaurantMenu.Catalog.Presentation.Items;
using RestaurantMenu.Restaurants.Infrastructure;
using RestaurantMenu.Restaurants.Presentation.Restaurants;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(
    options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "O";
        options.UseUtcTimestamp = true;
    });

var restaurantsConnectionString = builder.Configuration.GetConnectionString("Restaurants") 
    ?? throw new InvalidOperationException(
        "Connection string 'Restaurants' is not configured.");

var catalogConnectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException(
        "Connection string 'Catalog' is not configured.");

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
builder.Services.AddRestaurantMenuObservability(
    builder.Configuration);
builder.Services.AddRestaurantsInfrastructure(restaurantsConnectionString);
builder.Services.AddCatalogInfrastructure(catalogConnectionString);
builder.Services.AddScoped<
    IRestaurantExistenceChecker,
    RestaurantExistenceChecker>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapRestaurantsEndpoints();
app.MapMenuCategoryEndpoints();
app.MapMenuItemEndpoints();

app.Run();

public partial class Program
{
}
