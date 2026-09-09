using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Application.Assets;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Presentation.Assets;

public static class MediaAssetEndpoints
{
    public static IEndpointRouteBuilder MapMediaAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/restaurants/{restaurantId:guid}/media-assets", InitiateAsync)
            .WithName("InitiateMediaUpload").WithTags("Media").Produces<InitiateUploadResponse>(201)
            .ProducesValidationProblem().ProducesProblem(409).RequireRestaurantAccess(Permissions.MediaWrite);
        endpoints.MapGet("/api/public/restaurants/{restaurantId:guid}/media-assets/{assetId:guid}", ResolvePublicAsync)
            .WithName("ResolvePublicMediaAsset").WithTags("Media").Produces(302).ProducesProblem(404).AllowAnonymous();
        endpoints.MapPost("/api/restaurants/{restaurantId:guid}/media-assets/{assetId:guid}/complete", CompleteAsync)
            .WithName("CompleteMediaUpload").WithTags("Media").Produces<MediaAssetResponse>()
            .ProducesProblem(404).ProducesProblem(409).RequireRestaurantAccess(Permissions.MediaWrite);
        endpoints.MapGet("/api/restaurants/{restaurantId:guid}/media-assets/{assetId:guid}", GetAsync)
            .WithName("GetMediaAsset").WithTags("Media").Produces<MediaAssetResponse>()
            .ProducesProblem(404).RequireRestaurantAccess(Permissions.MediaRead);
        endpoints.MapDelete("/api/restaurants/{restaurantId:guid}/media-assets/{assetId:guid}", DeleteAsync)
            .WithName("DeleteMediaAsset").WithTags("Media").Produces(204)
            .ProducesProblem(404).ProducesProblem(409).RequireRestaurantAccess(Permissions.MediaWrite);
        endpoints.MapPost("/api/restaurants/{restaurantId:guid}/media-assets/{assetId:guid}/reject", RejectAsync)
            .WithName("RejectMediaAsset").WithTags("Media").Produces(204)
            .ProducesProblem(404).ProducesProblem(409).RequireRestaurantAccess(Permissions.MediaWrite);
        return endpoints;
    }

    private static async Task<IResult> InitiateAsync(Guid restaurantId, InitiateUploadRequest request,
        ICurrentUser currentUser, ICommandHandler<InitiateUploadCommand, Result<InitiateUploadResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new InitiateUploadCommand(restaurantId, request.OriginalFileName,
            request.ContentType, request.SizeBytes, request.Sha256, currentUser.Subject), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Created(
            $"/api/restaurants/{restaurantId}/media-assets/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> ResolvePublicAsync(Guid restaurantId, Guid assetId,
        IQueryHandler<ResolvePublicMediaQuery, Result<Uri>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResolvePublicMediaQuery(restaurantId, new MediaAssetId(assetId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Redirect(result.Value.ToString(), permanent: false, preserveMethod: false);
    }

    private static async Task<IResult> CompleteAsync(Guid restaurantId, Guid assetId, CompleteUploadRequest request,
        ICommandHandler<CompleteUploadCommand, Result<MediaAssetResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CompleteUploadCommand(restaurantId, new MediaAssetId(assetId), request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAsync(Guid restaurantId, Guid assetId,
        IQueryHandler<GetMediaAssetQuery, Result<MediaAssetResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMediaAssetQuery(restaurantId, new MediaAssetId(assetId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> DeleteAsync(Guid restaurantId, Guid assetId, long expectedVersion,
        ICommandHandler<DeleteMediaAssetCommand, Result<MediaAssetId>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeleteMediaAssetCommand(restaurantId, new MediaAssetId(assetId), expectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
    }

    private static async Task<IResult> RejectAsync(Guid restaurantId, Guid assetId, RejectMediaAssetRequest request,
        ICommandHandler<RejectMediaAssetCommand, Result<MediaAssetId>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectMediaAssetCommand(restaurantId, new MediaAssetId(assetId), request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
    }
}

public sealed record InitiateUploadRequest(string? OriginalFileName, string? ContentType, long SizeBytes, string? Sha256);
public sealed record CompleteUploadRequest(long ExpectedVersion);
public sealed record RejectMediaAssetRequest(long ExpectedVersion);
