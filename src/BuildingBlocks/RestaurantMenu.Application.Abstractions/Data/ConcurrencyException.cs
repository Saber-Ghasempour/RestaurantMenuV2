namespace RestaurantMenu.Application.Abstractions.Data;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException()
    {
    }

    public ConcurrencyException(string message)
        : base(message)
    {
    }

    public ConcurrencyException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
