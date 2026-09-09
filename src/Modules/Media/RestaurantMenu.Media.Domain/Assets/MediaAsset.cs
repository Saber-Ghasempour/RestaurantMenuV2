using System.Text.RegularExpressions;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Domain.Assets;

public sealed partial class MediaAsset : AggregateRoot<MediaAssetId>
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int MaxFileNameLength = 255;
    public const int MaxStorageKeyLength = 512;
    public const int MaxContentTypeLength = 100;
    private static readonly HashSet<string> AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];

    private MediaAsset(MediaAssetId id, Guid restaurantId, string storageKey,
        string originalFileName, string contentType, long sizeBytes, string sha256,
        string createdBySubject, DateTimeOffset createdAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        CreatedBySubject = createdBySubject;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }
    public string StorageKey { get; }
    public string OriginalFileName { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string Sha256 { get; }
    public MediaAssetStatus Status { get; private set; }
    public string CreatedBySubject { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public long Version { get; private set; } = 1;

    public static Result<MediaAsset> Initiate(MediaAssetId id, Guid restaurantId,
        string? storageKey, string? originalFileName, string? contentType,
        long sizeBytes, string? sha256, string? createdBySubject, DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty) return Result.Failure<MediaAsset>(MediaAssetErrors.RestaurantRequired);
        var key = storageKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > MaxStorageKeyLength) return Result.Failure<MediaAsset>(MediaAssetErrors.StorageKeyRequired);
        var fileName = Path.GetFileName(originalFileName?.Trim())?.Trim();
        if (string.IsNullOrEmpty(fileName) || fileName.Length > MaxFileNameLength) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidFileName);
        var type = contentType?.Trim().ToLowerInvariant();
        if (type is null || !AllowedContentTypes.Contains(type)) return Result.Failure<MediaAsset>(MediaAssetErrors.UnsupportedContentType);
        if (sizeBytes is <= 0 or > MaxSizeBytes) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidSize);
        var checksum = sha256?.Trim().ToLowerInvariant();
        if (checksum is null || !Sha256Regex().IsMatch(checksum)) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidChecksum);
        var subject = createdBySubject?.Trim();
        if (string.IsNullOrEmpty(subject) || subject.Length > 255) return Result.Failure<MediaAsset>(MediaAssetErrors.SubjectRequired);
        return Result.Success(new MediaAsset(id, restaurantId, key, fileName, type, sizeBytes, checksum, subject, createdAtUtc));
    }

    public Result<MediaAsset> Complete(string contentType, long sizeBytes, string sha256, int width, int height)
    {
        if (Status != MediaAssetStatus.Pending) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidState);
        if (!string.Equals(ContentType, contentType, StringComparison.OrdinalIgnoreCase) ||
            SizeBytes != sizeBytes || !string.Equals(Sha256, sha256, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<MediaAsset>(MediaAssetErrors.UploadedObjectMismatch);
        if (width <= 0 || height <= 0) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidDimensions);
        Width = width;
        Height = height;
        Status = MediaAssetStatus.Ready;
        Version++;
        return Result.Success(this);
    }

    public Result<MediaAsset> Reject(DateTimeOffset rejectedAtUtc)
    {
        if (Status != MediaAssetStatus.Pending) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidState);
        Status = MediaAssetStatus.Rejected;
        DeletedAtUtc = rejectedAtUtc;
        Version++;
        return Result.Success(this);
    }

    public Result<MediaAsset> Delete(DateTimeOffset deletedAtUtc)
    {
        if (Status == MediaAssetStatus.Deleted) return Result.Success(this);
        if (Status is not (MediaAssetStatus.Ready or MediaAssetStatus.Rejected)) return Result.Failure<MediaAsset>(MediaAssetErrors.InvalidState);
        Status = MediaAssetStatus.Deleted;
        DeletedAtUtc = deletedAtUtc;
        Version++;
        return Result.Success(this);
    }

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
