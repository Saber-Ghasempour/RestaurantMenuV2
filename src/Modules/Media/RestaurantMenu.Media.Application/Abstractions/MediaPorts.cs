using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Application.Abstractions;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetAsync(MediaAssetId id, CancellationToken cancellationToken);
    Task<MediaAsset?> FindActiveByChecksumAsync(Guid restaurantId, string sha256, CancellationToken cancellationToken);
    void Add(MediaAsset asset);
}

public interface IMediaAssetReadService
{
    Task<MediaAssetResponse?> GetAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaAsset>> GetExpiredPendingAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}

public interface IMediaUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class DuplicateMediaChecksumException(Exception innerException) : Exception("An active media checksum already exists.", innerException);

public interface IObjectStorage
{
    Task<Uri> CreateUploadUrlAsync(string key, string contentType, TimeSpan lifetime, CancellationToken cancellationToken);
    Task<StoredObjectMetadata?> InspectAsync(string key, long maximumBytes, CancellationToken cancellationToken);
    Task<Uri> CreateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed record StoredObjectMetadata(string ContentType, long SizeBytes, string Sha256, int Width, int Height);

public interface IMediaReferenceChecker
{
    Task<bool> IsReferencedAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken);
}

public interface IPublicMediaReferenceChecker
{
    Task<bool> IsPubliclyReferencedAsync(Guid restaurantId, MediaAssetId id, CancellationToken cancellationToken);
}

public sealed record MediaAssetResponse(Guid Id, Guid RestaurantId, string OriginalFileName,
    string ContentType, long SizeBytes, int? Width, int? Height, string Sha256,
    string Status, DateTimeOffset CreatedAtUtc, long Version, string? Url = null);
