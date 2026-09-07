using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.DeleteRestaurant;

public sealed class DeleteRestaurantCommandHandler
    : ICommandHandler<
        DeleteRestaurantCommand,
        Result<RestaurantId>>
{
    private readonly IRestaurantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IRestaurantCache _cache;

    public DeleteRestaurantCommandHandler(
        IRestaurantRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IRestaurantCache cache)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(cache);
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _cache = cache;
    }

    public async Task<Result<RestaurantId>> Handle(
        DeleteRestaurantCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var restaurant = await _repository.GetByIdAsync(
            command.RestaurantId,
            cancellationToken);

        if (restaurant is null)
        {
            return Result.Failure<RestaurantId>(
                RestaurantErrors.NotFound(command.RestaurantId));
        }

        if (restaurant.Version != command.ExpectedVersion)
        {
            return Result.Failure<RestaurantId>(
                RestaurantErrors.VersionConflict(restaurant.Id));
        }

        restaurant.Delete(_timeProvider.GetUtcNow());

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<RestaurantId>(
                RestaurantErrors.VersionConflict(restaurant.Id));
        }

        await _cache.RemoveAsync(
            restaurant.Id,
            cancellationToken);

        return Result.Success(restaurant.Id);
    }
}
