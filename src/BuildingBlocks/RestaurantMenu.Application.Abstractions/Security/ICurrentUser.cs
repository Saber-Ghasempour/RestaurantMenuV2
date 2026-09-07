namespace RestaurantMenu.Application.Abstractions.Security;

public interface ICurrentUser
{
    string Subject { get; }
}
