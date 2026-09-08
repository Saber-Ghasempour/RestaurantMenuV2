using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantLinks;

public sealed class UpdateRestaurantLinksCommandHandler
    : ICommandHandler<
        UpdateRestaurantLinksCommand,
        Result<long>>
{
    private readonly IRestaurantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRestaurantCache _cache;

    public UpdateRestaurantLinksCommandHandler(
        IRestaurantRepository repository,
        IUnitOfWork unitOfWork,
        IRestaurantCache cache)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(cache);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<long>> Handle(
        UpdateRestaurantLinksCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var restaurant =
            await _repository.GetByIdAsync(
                command.RestaurantId,
                cancellationToken);

        if (restaurant is null)
        {
            return Result.Failure<long>(
                RestaurantErrors.NotFound(
                    command.RestaurantId));
        }

        if (restaurant.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(
                RestaurantErrors.VersionConflict(
                    restaurant.Id));
        }

        var linksResult = restaurant.UpdateLinks(
            command.WebsiteUrl,
            command.InstagramUrl,
            command.FacebookUrl,
            command.WhatsAppUrl,
            command.TelegramUrl,
            command.TwitterUrl);

        if (linksResult.IsFailure)
        {
            return Result.Failure<long>(
                linksResult.Error);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(
                RestaurantErrors.VersionConflict(
                    restaurant.Id));
        }

        await _cache.RemoveAsync(
            restaurant.Id,
            cancellationToken);

        return Result.Success(restaurant.Version);
    }
}
