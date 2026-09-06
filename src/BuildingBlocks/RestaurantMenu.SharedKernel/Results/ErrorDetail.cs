namespace RestaurantMenu.SharedKernel.Results;

public sealed record ErrorDetail(
    string Code,
    string Description,
    ErrorType Type)
{
    public static readonly ErrorDetail None = new(
        string.Empty,
        string.Empty,
        ErrorType.Failure);

    public static ErrorDetail Failure(
        string code,
        string description) =>
        new(code, description, ErrorType.Failure);

    public static ErrorDetail Validation(
        string code,
        string description) =>
        new(code, description, ErrorType.Validation);

    public static ErrorDetail NotFound(
        string code,
        string description) =>
        new(code, description, ErrorType.NotFound);

    public static ErrorDetail Conflict(
        string code,
        string description) =>
        new(code, description, ErrorType.Conflict);
}