namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public sealed class SlugAlreadyExistsException : Exception
{
    public SlugAlreadyExistsException() { }
    public SlugAlreadyExistsException(string message) : base(message) { }
    public SlugAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}
