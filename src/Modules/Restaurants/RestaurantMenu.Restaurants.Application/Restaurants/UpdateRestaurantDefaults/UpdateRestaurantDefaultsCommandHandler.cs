using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantDefaults;

public sealed class UpdateRestaurantDefaultsCommandHandler(
    IRestaurantRepository repository,
    IUnitOfWork unitOfWork,
    IRestaurantCache cache,
    IRestaurantPublicMenuInvalidator publicMenuInvalidator)
    : ICommandHandler<UpdateRestaurantDefaultsCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        UpdateRestaurantDefaultsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var restaurant = await repository.GetByIdAsync(command.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            return Result.Failure<long>(RestaurantErrors.NotFound(command.RestaurantId));
        }

        if (restaurant.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(RestaurantErrors.VersionConflict(restaurant.Id));
        }

        var previousVersion = restaurant.Version;
        var updateResult = restaurant.UpdateDefaults(
            command.DefaultCurrency, command.DefaultLocale, command.TimeZoneId);
        if (updateResult.IsFailure)
        {
            return Result.Failure<long>(updateResult.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(RestaurantErrors.VersionConflict(restaurant.Id));
        }

        if (restaurant.Version != previousVersion)
        {
            await cache.RemoveAsync(restaurant.Id, cancellationToken);
            await publicMenuInvalidator.InvalidateAsync(
                restaurant.Id, cancellationToken);
        }
        return Result.Success(restaurant.Version);
    }
}
