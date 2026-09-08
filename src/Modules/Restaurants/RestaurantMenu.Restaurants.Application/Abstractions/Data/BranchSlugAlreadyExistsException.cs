namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public sealed class BranchSlugAlreadyExistsException : Exception
{
    public BranchSlugAlreadyExistsException() { }
    public BranchSlugAlreadyExistsException(string message) : base(message) { }
    public BranchSlugAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException) { }
}
