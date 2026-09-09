using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Media;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.SetRestaurantBranding;

public sealed class SetRestaurantBrandingCommandHandler(IRestaurantRepository repository, IUnitOfWork unitOfWork,
    IRestaurantCache cache, IMediaAssetValidator mediaValidator)
    : ICommandHandler<SetRestaurantBrandingCommand, Result<long>>
{
    public async Task<Result<long>> Handle(SetRestaurantBrandingCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await repository.GetByIdAsync(command.RestaurantId, cancellationToken);
        if (restaurant is null) return Result.Failure<long>(RestaurantErrors.NotFound(command.RestaurantId));
        if (restaurant.Version != command.ExpectedVersion) return Result.Failure<long>(RestaurantErrors.VersionConflict(restaurant.Id));
        foreach (var id in new[] { command.LogoMediaId, command.CoverMediaId }.OfType<Guid>().Distinct())
            if (!await mediaValidator.IsReadyAsync(command.RestaurantId.Value, id, cancellationToken))
                return Result.Failure<long>(ErrorDetail.NotFound("Media.AssetNotFound", $"Ready media asset '{id}' was not found."));
        var branding = restaurant.SetBranding(command.LogoMediaId, command.CoverMediaId);
        if (branding.IsFailure) return Result.Failure<long>(branding.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException) { return Result.Failure<long>(RestaurantErrors.VersionConflict(restaurant.Id)); }
        await cache.RemoveAsync(restaurant.Id, cancellationToken);
        return Result.Success(restaurant.Version);
    }
}
