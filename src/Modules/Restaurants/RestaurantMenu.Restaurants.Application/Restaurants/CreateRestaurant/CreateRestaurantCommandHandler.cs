using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;

public sealed class CreateRestaurantCommandHandler :
    ICommandHandler<CreateRestaurantCommand, Result<RestaurantId>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IRestaurantMembershipRepository _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        IRestaurantMembershipRepository membershipRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(restaurantRepository);
        ArgumentNullException.ThrowIfNull(membershipRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _restaurantRepository = restaurantRepository;
        _membershipRepository = membershipRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RestaurantId>> Handle(
        CreateRestaurantCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var restaurantResult = Restaurant.Create(
            RestaurantId.New(),
            command.Name,
            _timeProvider.GetUtcNow());

        if (restaurantResult.IsFailure)
        {
            return Result.Failure<RestaurantId>(
                restaurantResult.Error);
        }

        var restaurant = restaurantResult.Value;
        var membershipResult = RestaurantMembership.Create(
            restaurant.Id,
            command.OwnerSubject,
            RestaurantMembershipRole.Owner,
            restaurant.CreatedAtUtc);

        if (membershipResult.IsFailure)
        {
            return Result.Failure<RestaurantId>(
                membershipResult.Error);
        }

        _restaurantRepository.Add(restaurant);
        _membershipRepository.Add(membershipResult.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success(restaurant.Id);
    }
}
