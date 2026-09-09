using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Media.Infrastructure.Database;

namespace RestaurantMenu.Media.Infrastructure.Assets;

public sealed class MediaAssetRepository(MediaDbContext dbContext) : IMediaAssetRepository, IMediaAssetReadService
{
    public Task<MediaAsset?> GetAsync(MediaAssetId id, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<MediaAsset?> FindActiveByChecksumAsync(Guid restaurantId, string sha256, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.SingleOrDefaultAsync(x => x.RestaurantId == restaurantId && x.Sha256 == sha256 && x.Status != MediaAssetStatus.Deleted, cancellationToken);
    public void Add(MediaAsset asset) => dbContext.MediaAssets.Add(asset);
    async Task<MediaAssetResponse?> IMediaAssetReadService.GetAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken) =>
        await dbContext.MediaAssets.AsNoTracking().Where(x => x.RestaurantId == restaurantId && x.Id == id && x.Status != MediaAssetStatus.Deleted)
            .Select(x => new MediaAssetResponse(x.Id.Value, x.RestaurantId, x.OriginalFileName, x.ContentType, x.SizeBytes,
                x.Width, x.Height, x.Sha256, x.Status.ToString(), x.CreatedAtUtc, x.Version, null)).SingleOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<MediaAsset>> GetExpiredPendingAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        await dbContext.MediaAssets.Where(x =>
            (x.Status == MediaAssetStatus.Pending && x.CreatedAtUtc < cutoff) ||
            ((x.Status == MediaAssetStatus.Rejected || x.Status == MediaAssetStatus.Deleted) && x.DeletedAtUtc < cutoff))
            .ToArrayAsync(cancellationToken);
}
