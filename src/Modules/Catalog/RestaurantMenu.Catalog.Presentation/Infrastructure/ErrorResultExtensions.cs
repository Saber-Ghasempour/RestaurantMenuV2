using Microsoft.AspNetCore.Http;

using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Infrastructure;

internal static class ErrorResultExtensions
{
    internal static IResult ToProblem(this ErrorDetail error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Type switch
        {
            ErrorType.Validation => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [error.Code] = [error.Description]
                }),
            ErrorType.NotFound => CreateProblem(
                error,
                StatusCodes.Status404NotFound),
            ErrorType.Conflict => CreateProblem(
                error,
                StatusCodes.Status409Conflict),
            _ => CreateProblem(
                error,
                StatusCodes.Status500InternalServerError)
        };
    }

    private static IResult CreateProblem(
        ErrorDetail error,
        int statusCode) =>
        Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description);
}
