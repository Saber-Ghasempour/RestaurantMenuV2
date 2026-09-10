using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;

using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Application.Abstractions.Security;

namespace RestaurantMenu.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddRestaurantMenuAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var authenticationOptions =
            configuration
                .GetRequiredSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>()
            ?? throw new InvalidOperationException(
                "Authentication configuration is missing.");

        Validate(authenticationOptions);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                options =>
                {
                    options.MetadataAddress =
                        authenticationOptions.MetadataAddress;
                    options.Audience = authenticationOptions.Audience;
                    options.RequireHttpsMetadata =
                        authenticationOptions.RequireHttpsMetadata;
                    options.MapInboundClaims = false;
                    options.SaveToken = false;
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = authenticationOptions.ValidIssuer,
                            ValidateAudience = true,
                            ValidAudience = authenticationOptions.Audience,
                            ValidateIssuerSigningKey = true,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(30),
                            NameClaimType = "preferred_username"
                        };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (token.Count == 1 &&
                                context.HttpContext.Request.Path.StartsWithSegments(
                                    "/hubs/staff-order-notifications"))
                                context.Token = token[0];
                            return Task.CompletedTask;
                        }
                    };
                });

        var fallbackPolicy =
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

        var authorizationBuilder =
            services
                .AddAuthorizationBuilder()
                .SetFallbackPolicy(fallbackPolicy);

        foreach (var permission in Permissions.All)
        {
            authorizationBuilder.AddPolicy(
                permission,
                policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .RequireClaim("sub")
                        .RequireClaim(
                            PermissionClaimTypes.Permission,
                            permission));
        }

        authorizationBuilder.AddPolicy(
            RestaurantAccessPolicy.Name,
            policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("sub")
                    .AddRequirements(
                        new RestaurantAccessRequirement()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<
            IAuthorizationHandler,
            RestaurantAccessAuthorizationHandler>();

        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            AuthorizationProblemDetailsResultHandler>();

        return services;
    }

    private static void Validate(
        AuthenticationOptions options)
    {
        ValidateAbsoluteUri(
            options.MetadataAddress,
            nameof(options.MetadataAddress),
            options.RequireHttpsMetadata);
        ValidateAbsoluteUri(
            options.ValidIssuer,
            nameof(options.ValidIssuer),
            options.RequireHttpsMetadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
    }

    private static void ValidateAbsoluteUri(
        string value,
        string name,
        bool requireHttps)
    {
        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri))
        {
            throw new InvalidOperationException(
                $"Authentication {name} must be an absolute URI.");
        }

        if (requireHttps &&
            !string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Authentication {name} must use HTTPS when metadata HTTPS is required.");
        }
    }
}
