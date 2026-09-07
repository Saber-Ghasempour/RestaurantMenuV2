using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantMenu.Api.Authentication;

public sealed class AuthorizationProblemDetailsResultHandler(
    IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await context.ChallengeAsync();
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "A valid access token is required.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.2");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await context.ForbidAsync();
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "The access token does not grant the required permission.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4");
            return;
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            authorizeResult);
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string type)
    {
        context.Response.StatusCode = statusCode;

        var written = await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Type = type
                }
            });

        if (!written)
        {
            await context.Response.WriteAsync(title);
        }
    }
}
