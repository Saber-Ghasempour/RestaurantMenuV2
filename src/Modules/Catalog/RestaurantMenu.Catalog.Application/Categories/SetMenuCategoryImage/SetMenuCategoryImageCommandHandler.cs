using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Media;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.SetMenuCategoryImage;

public sealed class SetMenuCategoryImageCommandHandler(IMenuCategoryRepository repository, ICatalogUnitOfWork unitOfWork,
    IMediaAssetValidator validator, IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<SetMenuCategoryImageCommand, Result<long>>
{
    public async Task<Result<long>> Handle(SetMenuCategoryImageCommand command, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null || category.RestaurantId != command.RestaurantId)
            return Result.Failure<long>(MenuCategoryApplicationErrors.CategoryNotFound(command.CategoryId));
        if (category.Version != command.ExpectedVersion)
            return Result.Failure<long>(MenuCategoryApplicationErrors.VersionConflict(category.Id));
        if (command.ImageMediaId.HasValue && !await validator.IsReadyAsync(command.RestaurantId, command.ImageMediaId.Value, cancellationToken))
            return Result.Failure<long>(ErrorDetail.NotFound("Media.AssetNotFound", $"Ready media asset '{command.ImageMediaId}' was not found."));
        var oldVersion = category.Version;
        var image = category.SetImage(command.ImageMediaId);
        if (image.IsFailure) return Result.Failure<long>(image.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException) { return Result.Failure<long>(MenuCategoryApplicationErrors.VersionConflict(category.Id)); }
        if (category.Version != oldVersion) await cacheInvalidator.InvalidateRestaurantAsync(command.RestaurantId, cancellationToken);
        return Result.Success(category.Version);
    }
}
