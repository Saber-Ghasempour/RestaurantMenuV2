using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Application.Assets;

public sealed record InitiateUploadCommand(Guid RestaurantId, string? OriginalFileName,
    string? ContentType, long SizeBytes, string? Sha256, string? Subject)
    : ICommand<Result<InitiateUploadResponse>>;
public sealed record InitiateUploadResponse(Guid Id, Uri UploadUrl, DateTimeOffset ExpiresAtUtc);
public sealed record CompleteUploadCommand(Guid RestaurantId, MediaAssetId AssetId, long ExpectedVersion)
    : ICommand<Result<MediaAssetResponse>>;
public sealed record DeleteMediaAssetCommand(Guid RestaurantId, MediaAssetId AssetId, long ExpectedVersion)
    : ICommand<Result<MediaAssetId>>;
public sealed record RejectMediaAssetCommand(Guid RestaurantId, MediaAssetId AssetId, long ExpectedVersion)
    : ICommand<Result<MediaAssetId>>;
public sealed record GetMediaAssetQuery(Guid RestaurantId, MediaAssetId AssetId)
    : IQuery<Result<MediaAssetResponse>>;
public sealed record ResolvePublicMediaQuery(Guid RestaurantId, MediaAssetId AssetId)
    : IQuery<Result<Uri>>;

public sealed class InitiateUploadCommandHandler(IMediaAssetRepository repository,
    IMediaUnitOfWork unitOfWork, IObjectStorage storage, TimeProvider timeProvider)
    : ICommandHandler<InitiateUploadCommand, Result<InitiateUploadResponse>>
{
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);
    public async Task<Result<InitiateUploadResponse>> Handle(InitiateUploadCommand command, CancellationToken cancellationToken)
    {
        var checksum = command.Sha256?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await repository.FindActiveByChecksumAsync(command.RestaurantId, checksum, cancellationToken) is not null)
            return Result.Failure<InitiateUploadResponse>(MediaApplicationErrors.DuplicateChecksum);
        var id = MediaAssetId.New();
        var key = $"restaurants/{command.RestaurantId:N}/{id.Value:N}";
        var result = MediaAsset.Initiate(id, command.RestaurantId, key, command.OriginalFileName,
            command.ContentType, command.SizeBytes, checksum, command.Subject, timeProvider.GetUtcNow());
        if (result.IsFailure) return Result.Failure<InitiateUploadResponse>(result.Error);
        var expires = timeProvider.GetUtcNow().Add(UploadLifetime);
        var url = await storage.CreateUploadUrlAsync(key, result.Value.ContentType, UploadLifetime, cancellationToken);
        repository.Add(result.Value);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DuplicateMediaChecksumException) { return Result.Failure<InitiateUploadResponse>(MediaApplicationErrors.DuplicateChecksum); }
        return Result.Success(new InitiateUploadResponse(id.Value, url, expires));
    }
}

public sealed class CompleteUploadCommandHandler(IMediaAssetRepository repository,
    IMediaUnitOfWork unitOfWork, IObjectStorage storage)
    : ICommandHandler<CompleteUploadCommand, Result<MediaAssetResponse>>
{
    public async Task<Result<MediaAssetResponse>> Handle(CompleteUploadCommand command, CancellationToken cancellationToken)
    {
        var asset = await repository.GetAsync(command.AssetId, cancellationToken);
        if (asset is null || asset.RestaurantId != command.RestaurantId)
            return Result.Failure<MediaAssetResponse>(MediaApplicationErrors.NotFound(command.AssetId));
        if (asset.Version != command.ExpectedVersion)
            return Result.Failure<MediaAssetResponse>(MediaApplicationErrors.VersionConflict(asset.Id));
        StoredObjectMetadata? metadata;
        try { metadata = await storage.InspectAsync(asset.StorageKey, MediaAsset.MaxSizeBytes, cancellationToken); }
        catch (InvalidDataException) { return Result.Failure<MediaAssetResponse>(MediaAssetErrors.UploadedObjectMismatch); }
        if (metadata is null) return Result.Failure<MediaAssetResponse>(MediaApplicationErrors.ObjectMissing);
        var completed = asset.Complete(metadata.ContentType, metadata.SizeBytes, metadata.Sha256, metadata.Width, metadata.Height);
        if (completed.IsFailure) return Result.Failure<MediaAssetResponse>(completed.Error);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var url = await storage.CreateReadUrlAsync(asset.StorageKey, TimeSpan.FromMinutes(5), cancellationToken);
        return Result.Success(ToResponse(asset, url.ToString()));
    }
    internal static MediaAssetResponse ToResponse(MediaAsset a, string? url = null) => new(a.Id.Value,
        a.RestaurantId, a.OriginalFileName, a.ContentType, a.SizeBytes, a.Width, a.Height,
        a.Sha256, a.Status.ToString(), a.CreatedAtUtc, a.Version, url);
}

public sealed class RejectMediaAssetCommandHandler(IMediaAssetRepository repository, IMediaUnitOfWork unitOfWork,
    IObjectStorage storage, TimeProvider timeProvider) : ICommandHandler<RejectMediaAssetCommand, Result<MediaAssetId>>
{
    public async Task<Result<MediaAssetId>> Handle(RejectMediaAssetCommand command, CancellationToken cancellationToken)
    {
        var asset = await repository.GetAsync(command.AssetId, cancellationToken);
        if (asset is null || asset.RestaurantId != command.RestaurantId) return Result.Failure<MediaAssetId>(MediaApplicationErrors.NotFound(command.AssetId));
        if (asset.Status == MediaAssetStatus.Rejected)
        {
            await storage.DeleteAsync(asset.StorageKey, cancellationToken);
            return Result.Success(asset.Id);
        }
        if (asset.Version != command.ExpectedVersion) return Result.Failure<MediaAssetId>(MediaApplicationErrors.VersionConflict(asset.Id));
        var result = asset.Reject(timeProvider.GetUtcNow());
        if (result.IsFailure) return Result.Failure<MediaAssetId>(result.Error);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(asset.StorageKey, cancellationToken);
        return Result.Success(asset.Id);
    }
}

public sealed class DeleteMediaAssetCommandHandler(IMediaAssetRepository repository,
    IMediaUnitOfWork unitOfWork, IMediaReferenceChecker referenceChecker,
    IObjectStorage storage, TimeProvider timeProvider)
    : ICommandHandler<DeleteMediaAssetCommand, Result<MediaAssetId>>
{
    public async Task<Result<MediaAssetId>> Handle(DeleteMediaAssetCommand command, CancellationToken cancellationToken)
    {
        var asset = await repository.GetAsync(command.AssetId, cancellationToken);
        if (asset is null || asset.RestaurantId != command.RestaurantId)
            return Result.Failure<MediaAssetId>(MediaApplicationErrors.NotFound(command.AssetId));
        if (asset.Status == MediaAssetStatus.Deleted)
        {
            await storage.DeleteAsync(asset.StorageKey, cancellationToken);
            return Result.Success(asset.Id);
        }
        if (asset.Version != command.ExpectedVersion)
            return Result.Failure<MediaAssetId>(MediaApplicationErrors.VersionConflict(asset.Id));
        if (await referenceChecker.IsReferencedAsync(command.RestaurantId, command.AssetId, cancellationToken))
            return Result.Failure<MediaAssetId>(MediaApplicationErrors.AssetReferenced);
        var deleted = asset.Delete(timeProvider.GetUtcNow());
        if (deleted.IsFailure) return Result.Failure<MediaAssetId>(deleted.Error);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(asset.StorageKey, cancellationToken);
        return Result.Success(asset.Id);
    }
}

public sealed class GetMediaAssetQueryHandler(IMediaAssetReadService readService, IObjectStorage storage,
    IMediaAssetRepository repository) : IQueryHandler<GetMediaAssetQuery, Result<MediaAssetResponse>>
{
    public async Task<Result<MediaAssetResponse>> Handle(GetMediaAssetQuery query, CancellationToken cancellationToken)
    {
        var response = await readService.GetAsync(query.RestaurantId, query.AssetId, cancellationToken);
        if (response is null) return Result.Failure<MediaAssetResponse>(MediaApplicationErrors.NotFound(query.AssetId));
        if (response.Status != nameof(MediaAssetStatus.Ready)) return Result.Success(response);
        var asset = await repository.GetAsync(query.AssetId, cancellationToken);
        var url = await storage.CreateReadUrlAsync(asset!.StorageKey, TimeSpan.FromMinutes(5), cancellationToken);
        return Result.Success(response with { Url = url.ToString() });
    }
}

public sealed class ResolvePublicMediaQueryHandler(IMediaAssetRepository repository,
    IPublicMediaReferenceChecker referenceChecker, IObjectStorage storage)
    : IQueryHandler<ResolvePublicMediaQuery, Result<Uri>>
{
    public async Task<Result<Uri>> Handle(ResolvePublicMediaQuery query, CancellationToken cancellationToken)
    {
        var asset = await repository.GetAsync(query.AssetId, cancellationToken);
        if (asset is null || asset.RestaurantId != query.RestaurantId || asset.Status != MediaAssetStatus.Ready ||
            !await referenceChecker.IsPubliclyReferencedAsync(query.RestaurantId, query.AssetId, cancellationToken))
            return Result.Failure<Uri>(MediaApplicationErrors.NotFound(query.AssetId));
        return Result.Success(await storage.CreateReadUrlAsync(asset.StorageKey, TimeSpan.FromMinutes(5), cancellationToken));
    }
}
