using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Domain.UnitTests.Assets;

public sealed class MediaAssetTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
    private const string Sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void InitiateShouldNormalizeMetadataAndStartPending()
    {
        var result = MediaAsset.Initiate(MediaAssetId.New(), Guid.CreateVersion7(),
            "tenant/key", " ../ Dish.PNG ", "IMAGE/PNG", 1024, Sha256.ToUpperInvariant(),
            "user-1", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dish.PNG", result.Value.OriginalFileName);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(Sha256, result.Value.Sha256);
        Assert.Equal(MediaAssetStatus.Pending, result.Value.Status);
        Assert.Equal(1, result.Value.Version);
    }

    [Theory]
    [InlineData("text/plain", 100)]
    [InlineData("image/png", 0)]
    [InlineData("image/jpeg", 10485761)]
    public void InitiateShouldRejectInvalidContent(string contentType, long size)
    {
        var result = MediaAsset.Initiate(MediaAssetId.New(), Guid.CreateVersion7(),
            "key", "file.png", contentType, size, Sha256, "user", Now);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ReadyTransitionShouldRequireMatchingVerifiedMetadata()
    {
        var asset = Create();

        Assert.True(asset.Complete("image/jpeg", 10, Sha256, 10, 10).IsFailure);
        Assert.Equal(MediaAssetStatus.Pending, asset.Status);
        Assert.True(asset.Complete("image/png", 1024, Sha256, 640, 480).IsSuccess);
        Assert.Equal(MediaAssetStatus.Ready, asset.Status);
        Assert.Equal(640, asset.Width);
        Assert.Equal(2, asset.Version);
    }

    [Fact]
    public void RejectAndDeleteShouldEnforceLifecycle()
    {
        var pending = Create();
        Assert.True(pending.Reject(Now.AddMinutes(1)).IsSuccess);
        Assert.Equal(MediaAssetStatus.Rejected, pending.Status);
        Assert.True(pending.Complete("image/png", 1024, Sha256, 1, 1).IsFailure);

        var ready = Create();
        ready.Complete("image/png", 1024, Sha256, 1, 1);
        Assert.True(ready.Delete(Now.AddMinutes(2)).IsSuccess);
        Assert.Equal(MediaAssetStatus.Deleted, ready.Status);
        Assert.NotNull(ready.DeletedAtUtc);
        Assert.True(ready.Delete(Now.AddMinutes(3)).IsSuccess);
        Assert.Equal(3, ready.Version);
    }

    private static MediaAsset Create() => MediaAsset.Initiate(
        MediaAssetId.New(), Guid.CreateVersion7(), "tenant/key", "file.png",
        "image/png", 1024, Sha256, "user", Now).Value;
}
