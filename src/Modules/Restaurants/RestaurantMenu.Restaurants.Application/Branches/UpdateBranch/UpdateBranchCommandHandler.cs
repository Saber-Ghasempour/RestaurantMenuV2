using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.UpdateBranch;

public sealed class UpdateBranchCommandHandler
    : ICommandHandler<UpdateBranchCommand, Result<long>>
{
    private readonly IBranchRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBranchCommandHandler(
        IBranchRepository repository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(
        UpdateBranchCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var branch = await _repository.GetByIdAsync(
            command.RestaurantId, command.BranchId, cancellationToken);
        if (branch is null)
        {
            return Result.Failure<long>(BranchErrors.NotFound(command.BranchId));
        }

        if (branch.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(BranchErrors.VersionConflict(branch.Id));
        }

        var updateResult = branch.Update(
            command.Name, command.Slug, command.Phone, command.AddressLine,
            command.CityName, command.RegionName, command.PostalCode,
            command.CountryCode, command.Latitude, command.Longitude,
            command.TimeZoneId);
        if (updateResult.IsFailure)
        {
            return Result.Failure<long>(updateResult.Error);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (BranchSlugAlreadyExistsException)
        {
            return Result.Failure<long>(BranchErrors.SlugAlreadyExists);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<long>(BranchErrors.VersionConflict(branch.Id));
        }

        return Result.Success(branch.Version);
    }
}
