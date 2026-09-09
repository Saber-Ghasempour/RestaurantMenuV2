using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Application;

public static class MediaApplicationErrors
{
    public static ErrorDetail NotFound(MediaAssetId id) => ErrorDetail.NotFound("Media.AssetNotFound", $"Media asset '{id.Value}' was not found.");
    public static readonly ErrorDetail DuplicateChecksum = ErrorDetail.Conflict("Media.DuplicateChecksum", "An active media asset with the same checksum already exists for this restaurant.");
    public static readonly ErrorDetail ObjectMissing = ErrorDetail.Validation("Media.ObjectMissing", "The uploaded object could not be found.");
    public static readonly ErrorDetail AssetReferenced = ErrorDetail.Conflict("Media.AssetReferenced", "The media asset is still referenced and cannot be deleted.");
    public static ErrorDetail VersionConflict(MediaAssetId id) => ErrorDetail.Conflict("Media.VersionConflict", $"Media asset '{id.Value}' was modified by another request.");
}
