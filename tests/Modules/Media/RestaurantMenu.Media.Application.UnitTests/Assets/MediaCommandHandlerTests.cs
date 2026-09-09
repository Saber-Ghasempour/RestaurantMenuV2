using RestaurantMenu.Media.Application;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Application.Assets;
using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Application.UnitTests.Assets;

public sealed class MediaCommandHandlerTests
{
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task InitiateShouldCreateOpaqueTenantKeyAndRejectDuplicate()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var handler = new InitiateUploadCommandHandler(repository, repository, storage, TimeProvider.System);
        var restaurantId = Guid.CreateVersion7();

        var result = await handler.Handle(new InitiateUploadCommand(restaurantId, "dish.png", "image/png", 24, Hash, "actor"), default);

        Assert.True(result.IsSuccess);
        Assert.StartsWith($"restaurants/{restaurantId:N}/", repository.Asset!.StorageKey);
        Assert.DoesNotContain("dish", repository.Asset.StorageKey, StringComparison.OrdinalIgnoreCase);
        var duplicate = await handler.Handle(new InitiateUploadCommand(restaurantId, "other.png", "image/png", 24, Hash, "actor"), default);
        Assert.Equal(MediaApplicationErrors.DuplicateChecksum, duplicate.Error);
    }

    [Fact]
    public async Task CompleteShouldHideWrongTenantAndUseVerifiedMetadata()
    {
        var repository = new FakeRepository { Asset = Create() };
        var storage = new FakeStorage { Metadata = new("image/png", 24, Hash, 2, 3) };
        var handler = new CompleteUploadCommandHandler(repository, repository, storage);

        var hidden = await handler.Handle(new CompleteUploadCommand(Guid.CreateVersion7(), repository.Asset.Id, 1), default);
        Assert.Equal("Media.AssetNotFound", hidden.Error.Code);

        var result = await handler.Handle(new CompleteUploadCommand(repository.Asset.RestaurantId, repository.Asset.Id, 1), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Width);
        Assert.Equal(MediaAssetStatus.Ready, repository.Asset.Status);
    }

    [Fact]
    public async Task DeleteShouldRefuseReferencedAsset()
    {
        var repository = new FakeRepository { Asset = Create() };
        repository.Asset.Complete("image/png", 24, Hash, 2, 3);
        var handler = new DeleteMediaAssetCommandHandler(repository, repository, new Referenced(), new FakeStorage(), TimeProvider.System);
        var result = await handler.Handle(new DeleteMediaAssetCommand(repository.Asset.RestaurantId, repository.Asset.Id, 2), default);
        Assert.Equal(MediaApplicationErrors.AssetReferenced, result.Error);
        Assert.Equal(MediaAssetStatus.Ready, repository.Asset.Status);
    }

    private static MediaAsset Create()
    {
        var restaurantId = Guid.CreateVersion7();
        return MediaAsset.Initiate(MediaAssetId.New(), restaurantId, $"restaurants/{restaurantId:N}/key",
            "dish.png", "image/png", 24, Hash, "actor", DateTimeOffset.UtcNow).Value;
    }

    private sealed class FakeRepository : IMediaAssetRepository, IMediaUnitOfWork
    {
        public MediaAsset? Asset { get; set; }
        public void Add(MediaAsset asset) => Asset = asset;
        public Task<MediaAsset?> GetAsync(MediaAssetId id, CancellationToken cancellationToken) => Task.FromResult(Asset?.Id == id ? Asset : null);
        public Task<MediaAsset?> FindActiveByChecksumAsync(Guid restaurantId, string sha256, CancellationToken cancellationToken) =>
            Task.FromResult(Asset is { Status: not MediaAssetStatus.Deleted } && Asset.RestaurantId == restaurantId && Asset.Sha256 == sha256 ? Asset : null);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class FakeStorage : IObjectStorage
    {
        public StoredObjectMetadata? Metadata { get; set; }
        public Task<Uri> CreateUploadUrlAsync(string key, string contentType, TimeSpan lifetime, CancellationToken cancellationToken) => Task.FromResult(new Uri("https://storage.test/upload"));
        public Task<StoredObjectMetadata?> InspectAsync(string key, long maximumBytes, CancellationToken cancellationToken) => Task.FromResult(Metadata);
        public Task<Uri> CreateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken) => Task.FromResult(new Uri("https://storage.test/read"));
        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class Referenced : IMediaReferenceChecker
    { public Task<bool> IsReferencedAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken) => Task.FromResult(true); }
}
