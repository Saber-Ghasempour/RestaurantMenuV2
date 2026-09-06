namespace RestaurantMenu.SharedKernel.Results;

public static class Result
{
    public static Result<T> Success<T>(T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(
            true,
            value,
            ErrorDetail.None);
    }

    public static Result<T> Failure<T>(ErrorDetail error)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error == ErrorDetail.None)
        {
            throw new ArgumentException(
                "A failed result must contain an error.",
                nameof(error));
        }

        return new Result<T>(
            false,
            default,
            error);
    }
}

public sealed class Result<T>
    where T : notnull
{
    private readonly T? _value;

    internal Result(
        bool isSuccess,
        T? value,
        ErrorDetail error)
    {
        if (isSuccess && error != ErrorDetail.None)
        {
            throw new InvalidOperationException(
                "A successful result cannot contain an error.");
        }

        if (!isSuccess && error == ErrorDetail.None)
        {
            throw new InvalidOperationException(
                "A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "The value of a failed result cannot be accessed.");

    public ErrorDetail Error { get; }
}