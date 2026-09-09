using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Application.DiningSessions.StartDiningSession;
using RestaurantMenu.Ordering.Domain.DiningSessions;

namespace RestaurantMenu.Ordering.Application.UnitTests.DiningSessions;

public sealed class StartDiningSessionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DineInCodeShouldCreateHashedFixedScopeSession()
    {
        var restaurantId = Guid.NewGuid(); var branchId = Guid.NewGuid(); var tableId = Guid.NewGuid();
        var repository = new RepositoryStub();
        var handler = new StartDiningSessionCommandHandler(
            new CodeResolverStub(new(restaurantId, branchId, tableId, PublicCodePurpose.DineInOrdering)),
            repository, repository, new TokenGeneratorStub("raw-session-token", new string('b', 64)),
            new FixedTimeProvider(Now), new DiningSessionOptions(TimeSpan.FromHours(2)));

        var result = await handler.Handle(new("raw-public-code"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("raw-session-token", result.Value.Token);
        Assert.Equal(Now.AddHours(2), result.Value.ExpiresAtUtc);
        Assert.Equal(new string('b', 64), repository.Added!.TokenHash);
        Assert.Equal(tableId, repository.Added.DiningTableId);
        Assert.DoesNotContain("raw-session-token", repository.Added.TokenHash, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PublicCodePurpose.MenuOnly)]
    [InlineData(PublicCodePurpose.DineInOrdering, false)]
    public async Task IneligibleOrInactiveCodeShouldNotCreateSession(PublicCodePurpose purpose, bool active = true)
    {
        var scope = active ? new PublicCodeScope(Guid.NewGuid(), purpose == PublicCodePurpose.DineInOrdering ? Guid.NewGuid() : null,
            purpose == PublicCodePurpose.DineInOrdering ? Guid.NewGuid() : null, purpose) : null;
        var repository = new RepositoryStub();
        var handler = new StartDiningSessionCommandHandler(new CodeResolverStub(scope), repository, repository,
            new TokenGeneratorStub("raw", new string('c', 64)), new FixedTimeProvider(Now), new DiningSessionOptions(TimeSpan.FromHours(2)));
        var result = await handler.Handle(new("code"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("DiningSession.InvalidPublicCode", result.Error.Code);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task TokenCollisionShouldRetryWithoutPersistingRawToken()
    {
        var repository = new RepositoryStub { ExistingHashes = 1 };
        var generator = new SequentialTokenGenerator();
        var handler = new StartDiningSessionCommandHandler(
            new CodeResolverStub(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PublicCodePurpose.DineInOrdering)),
            repository, repository, generator, new FixedTimeProvider(Now), new DiningSessionOptions(TimeSpan.FromMinutes(30)));
        var result = await handler.Handle(new("code"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("token-2", result.Value.Token);
        Assert.Equal("hash-2", repository.Added!.TokenHash);
    }

    [Fact]
    public async Task CapabilityResolutionShouldRejectWrongTableAndExactExpiryBoundary()
    {
        var scope = new DiningSessionScope(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Now.AddMinutes(30));
        var repository = new RepositoryStub { Resolved = scope };
        var handler = new ResolveDiningSessionQueryHandler(repository,
            new TokenGeneratorStub("raw", "hash"), new FixedTimeProvider(Now));
        var wrongTable = await handler.Handle(new("raw", ExpectedDiningTableId: Guid.NewGuid()), CancellationToken.None);
        Assert.True(wrongTable.IsFailure);

        repository.Resolved = null;
        var expired = await handler.Handle(new("raw"), CancellationToken.None);
        Assert.True(expired.IsFailure);
        Assert.Equal("DiningSession.InvalidCapability", expired.Error.Code);
    }

    [Fact]
    public async Task ReusingPrintedCodeShouldIssueIndependentBoundedSessions()
    {
        var repository = new RepositoryStub(); var generator = new SequentialTokenGenerator();
        var handler = new StartDiningSessionCommandHandler(
            new CodeResolverStub(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PublicCodePurpose.DineInOrdering)),
            repository, repository, generator, new FixedTimeProvider(Now), new DiningSessionOptions(TimeSpan.FromHours(2)));
        var first = await handler.Handle(new("printed-code"), CancellationToken.None);
        var second = await handler.Handle(new("printed-code"), CancellationToken.None);
        Assert.True(first.IsSuccess); Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Token, second.Value.Token);
        Assert.Equal(first.Value.ExpiresAtUtc, second.Value.ExpiresAtUtc);
    }

    private sealed class CodeResolverStub(PublicCodeScope? scope) : IPublicCodeResolver
    { public Task<PublicCodeScope?> ResolveAsync(string code, CancellationToken cancellationToken) => Task.FromResult(scope); }
    private sealed class RepositoryStub : IDiningSessionRepository, IOrderingUnitOfWork
    {
        public int ExistingHashes { get; set; }
        public DiningSession? Added { get; private set; }
        public DiningSessionScope? Resolved { get; set; }
        public void Add(DiningSession session) => Added = session;
        public Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken)
        { if (ExistingHashes-- > 0) return Task.FromResult(true); return Task.FromResult(false); }
        public Task<DiningSessionScope?> ResolveAsync(string tokenHash, DateTimeOffset utcNow, CancellationToken cancellationToken) => Task.FromResult(Resolved);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class TokenGeneratorStub(string raw, string hash) : IDiningSessionTokenGenerator
    { public string Generate() => raw; public string Hash(string token) => hash; public bool IsWellFormed(string token) => true; }
    private sealed class SequentialTokenGenerator : IDiningSessionTokenGenerator
    { private int _value; public string Generate() => $"token-{++_value}"; public string Hash(string token) => token.Replace("token", "hash", StringComparison.Ordinal); public bool IsWellFormed(string token) => true; }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
