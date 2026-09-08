using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.CreateBranch;

public sealed class CreateBranchCommandHandler
    : ICommandHandler<CreateBranchCommand, Result<BranchId>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateBranchCommandHandler(
        IRestaurantRepository restaurantRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(restaurantRepository);
        ArgumentNullException.ThrowIfNull(branchRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _restaurantRepository = restaurantRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<BranchId>> Handle(
        CreateBranchCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var restaurant = await _restaurantRepository.GetByIdAsync(
            command.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            return Result.Failure<BranchId>(
                RestaurantErrors.NotFound(command.RestaurantId));
        }

        var branchResult = Branch.Create(
            BranchId.New(), command.RestaurantId, command.Name, command.Slug,
            command.Phone, command.AddressLine, command.CityName,
            command.RegionName, command.PostalCode, command.CountryCode,
            command.Latitude, command.Longitude, command.TimeZoneId,
            _timeProvider.GetUtcNow());
        if (branchResult.IsFailure)
        {
            return Result.Failure<BranchId>(branchResult.Error);
        }

        _branchRepository.Add(branchResult.Value);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (BranchSlugAlreadyExistsException)
        {
            return Result.Failure<BranchId>(BranchErrors.SlugAlreadyExists);
        }

        return Result.Success(branchResult.Value.Id);
    }
}
