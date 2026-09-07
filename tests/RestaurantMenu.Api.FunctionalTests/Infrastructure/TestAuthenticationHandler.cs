using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RestaurantMenu.Presentation.Abstractions.Authorization;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    public const string AuthenticationScheme = "Test";

    public const string IdentityHeader = "X-Test-Identity";

    public const string PermissionsHeader = "X-Test-Permissions";

    public const string AnonymousIdentity = "anonymous";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(
                IdentityHeader,
                out var identityValues) &&
            string.Equals(
                identityValues.ToString(),
                AnonymousIdentity,
                StringComparison.Ordinal))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("preferred_username", "test-user")
        };

        var permissions = GetPermissions();

        claims.AddRange(
            permissions.Select(
                permission =>
                    new Claim(
                        PermissionClaimTypes.Permission,
                        permission)));

        var identity = new ClaimsIdentity(
            claims,
            AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(
        AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = AuthenticationScheme;

        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(
        AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    }

    private IEnumerable<string> GetPermissions()
    {
        if (!Request.Headers.TryGetValue(
                PermissionsHeader,
                out var permissionValues))
        {
            return Permissions.All;
        }

        return permissionValues
            .SelectMany(
                value =>
                    (value ?? string.Empty).Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries))
            .Where(
                permission =>
                    !string.Equals(
                        permission,
                        "none",
                        StringComparison.Ordinal));
    }
}
