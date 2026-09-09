namespace RestaurantMenu.Ordering.Application.Abstractions;
public interface IDiningSessionTokenGenerator
{
    string Generate();
    string Hash(string token);
    bool IsWellFormed(string token);
}
