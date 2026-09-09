using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Media;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.SetMenuItemMedia;

public sealed class SetMenuItemMediaCommandHandler(IMenuItemRepository itemRepository,
    IMenuItemMediaRepository mediaRepository, IMediaAssetValidator validator,
    ICatalogUnitOfWork unitOfWork, IPublicMenuCacheInvalidator cacheInvalidator)
    : ICommandHandler<SetMenuItemMediaCommand, Result<long>>
{
    public async Task<Result<long>> Handle(SetMenuItemMediaCommand command, CancellationToken cancellationToken)
    {
        var item = await itemRepository.GetByIdAsync(command.MenuItemId, cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId || item.CategoryId != command.CategoryId)
            return Result.Failure<long>(MenuItemApplicationErrors.ItemNotFound(command.MenuItemId));
        if (item.Version != command.ExpectedVersion) return Result.Failure<long>(MenuItemApplicationErrors.VersionConflict(item.Id));
        if (command.Media.Count > 10 || command.Media.Select(x => x.MediaAssetId).Distinct().Count() != command.Media.Count ||
            command.Media.Select(x => x.DisplayOrder).Distinct().Count() != command.Media.Count || command.Media.Count(x => x.IsPrimary) > 1)
            return Result.Failure<long>(ErrorDetail.Validation("Catalog.InvalidMenuItemMediaSet", "Supply at most ten unique assets and display orders, with at most one primary image."));
        var entries = new List<MenuItemMedia>(command.Media.Count);
        foreach (var input in command.Media)
        {
            if (!await validator.IsReadyAsync(command.RestaurantId, input.MediaAssetId, cancellationToken))
                return Result.Failure<long>(ErrorDetail.NotFound("Media.AssetNotFound", $"Ready media asset '{input.MediaAssetId}' was not found."));
            var created = MenuItemMedia.Create(command.RestaurantId, item.Id, input.MediaAssetId, input.DisplayOrder, input.AltText, input.IsPrimary);
            if (created.IsFailure) return Result.Failure<long>(created.Error);
            entries.Add(created.Value);
        }
        await mediaRepository.ReplaceAsync(command.RestaurantId, item.Id, entries, cancellationToken);
        item.MarkMediaChanged();
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException) { return Result.Failure<long>(MenuItemApplicationErrors.VersionConflict(item.Id)); }
        await cacheInvalidator.InvalidateRestaurantAsync(command.RestaurantId, cancellationToken);
        return Result.Success(item.Version);
    }
}
