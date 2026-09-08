namespace RestaurantMenu.Restaurants.Application.Abstractions.Security;

public interface IPublicMenuCodeGenerator
{
    string Generate();
    string Hash(string code);
}
