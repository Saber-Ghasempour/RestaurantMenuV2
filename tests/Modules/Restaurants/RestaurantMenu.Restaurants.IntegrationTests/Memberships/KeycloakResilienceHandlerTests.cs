using System.Net;
using RestaurantMenu.Restaurants.Infrastructure.Memberships;

namespace RestaurantMenu.Restaurants.IntegrationTests.Memberships;

public sealed class KeycloakResilienceHandlerTests
{
    [Fact]
    public async Task SafeRequestRetriesTransientFailure()
    {
        var terminal = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(new KeycloakResilienceHandler(
            new KeycloakResilienceOptions(3, TimeSpan.Zero)) { InnerHandler = terminal });

        using var response = await client.GetAsync("https://identity.example/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, terminal.Attempts);
    }

    [Fact]
    public async Task UnsafeRequestDoesNotRetryTransientFailure()
    {
        var terminal = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.Created));
        using var client = new HttpClient(new KeycloakResilienceHandler(
            new KeycloakResilienceOptions(3, TimeSpan.Zero)) { InnerHandler = terminal });

        using var response = await client.PostAsync("https://identity.example/users", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, terminal.Attempts);
    }

    [Fact]
    public async Task PermanentFailureIsNotRetried()
    {
        var terminal = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var client = new HttpClient(new KeycloakResilienceHandler(
            new KeycloakResilienceOptions(3, TimeSpan.Zero)) { InnerHandler = terminal });

        using var response = await client.GetAsync("https://identity.example/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, terminal.Attempts);
    }

    [Fact]
    public async Task CallerCancellationStopsRetryPipeline()
    {
        var terminal = new CancellingHandler();
        using var client = new HttpClient(new KeycloakResilienceHandler(
            new KeycloakResilienceOptions(3, TimeSpan.Zero)) { InnerHandler = terminal });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetAsync("https://identity.example/users", cancellation.Token));
        Assert.Equal(1, terminal.Attempts);
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class CancellingHandler : HttpMessageHandler
    {
        public int Attempts { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
        }
    }
}
