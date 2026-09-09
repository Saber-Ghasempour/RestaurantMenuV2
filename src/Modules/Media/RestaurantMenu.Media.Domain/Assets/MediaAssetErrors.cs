using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Domain.Assets;

public static class MediaAssetErrors
{
    public static readonly ErrorDetail RestaurantRequired = ErrorDetail.Validation("Media.RestaurantRequired", "A restaurant identifier is required.");
    public static readonly ErrorDetail StorageKeyRequired = ErrorDetail.Validation("Media.StorageKeyRequired", "A storage key is required.");
    public static readonly ErrorDetail InvalidFileName = ErrorDetail.Validation("Media.InvalidFileName", "The file name must contain between 1 and 255 characters.");
    public static readonly ErrorDetail UnsupportedContentType = ErrorDetail.Validation("Media.UnsupportedContentType", "Only PNG, JPEG, and WebP images are supported.");
    public static readonly ErrorDetail InvalidSize = ErrorDetail.Validation("Media.InvalidSize", $"The file size must be between 1 byte and {MediaAsset.MaxSizeBytes} bytes.");
    public static readonly ErrorDetail InvalidChecksum = ErrorDetail.Validation("Media.InvalidChecksum", "SHA-256 must contain exactly 64 lowercase hexadecimal characters.");
    public static readonly ErrorDetail SubjectRequired = ErrorDetail.Validation("Media.SubjectRequired", "An authenticated subject is required.");
    public static readonly ErrorDetail InvalidState = ErrorDetail.Conflict("Media.InvalidState", "The media asset is not in a valid state for this operation.");
    public static readonly ErrorDetail UploadedObjectMismatch = ErrorDetail.Validation("Media.UploadedObjectMismatch", "The uploaded object does not match its declared metadata.");
    public static readonly ErrorDetail InvalidDimensions = ErrorDetail.Validation("Media.InvalidDimensions", "Image dimensions must be positive.");
}
