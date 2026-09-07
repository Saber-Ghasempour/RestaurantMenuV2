using System.Security.Claims;

using RestaurantMenu.Application.Abstractions.Security;

namespace RestaurantMenu.Api.Authentication;

public sealed class HttpContextCurrentUser(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public string Subject =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException(
            "The authenticated principal does not contain a subject claim.");
}
