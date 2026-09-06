using RestaurantMenu.Restaurants.Infrastructure;
using RestaurantMenu.Restaurants.Presentation.Restaurants;

var builder = WebApplication.CreateBuilder(args);

var restaurantsConnectionString = builder.Configuration.GetConnectionString("Restaurants") 
    ?? throw new InvalidOperationException(
        "Connection string 'Restaurants' is not configured.");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddRestaurantsInfrastructure(restaurantsConnectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapRestaurantsEndpoints();

app.Run();

public partial class Program
{
}