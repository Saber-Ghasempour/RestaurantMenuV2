using System.Net;

namespace RestaurantMenu.Restaurants.Infrastructure.Memberships;

public sealed record KeycloakResilienceOptions(int MaximumAttempts, TimeSpan RetryDelay);

public sealed class KeycloakResilienceHandler(KeycloakResilienceOptions options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var canRetry = request.Method is { } method &&
            (method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options);
        var attempts = canRetry ? Math.Max(1, options.MaximumAttempts) : 1;

        for (var attempt = 1; ; attempt++)
        {
            var outgoing = attempt == 1 ? request : Clone(request);
            try
            {
                var response = await base.SendAsync(outgoing, cancellationToken);
                if (attempt == attempts || !IsTransient(response.StatusCode)) return response;
                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < attempts)
            {
                // A safe request can be replayed after a transient transport failure.
            }

            if (options.RetryDelay > TimeSpan.Zero)
                await Task.Delay(options.RetryDelay, cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        (int)statusCode == 429 ||
        (int)statusCode >= 500;

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };
        foreach (var header in request.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        foreach (var option in request.Options) clone.Options.Set(new(option.Key), option.Value);
        return clone;
    }
}
