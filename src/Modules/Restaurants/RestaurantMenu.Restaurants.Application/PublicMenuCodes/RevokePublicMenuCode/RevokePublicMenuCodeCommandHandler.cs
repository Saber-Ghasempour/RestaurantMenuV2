using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.RevokePublicMenuCode;
public sealed class RevokePublicMenuCodeCommandHandler(IPublicMenuCodeRepository codes,
    IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<RevokePublicMenuCodeCommand, Result<long>>
{
    public async Task<Result<long>> Handle(RevokePublicMenuCodeCommand command, CancellationToken cancellationToken)
    {
        var code = await codes.GetByIdAsync(command.RestaurantId, command.PublicMenuCodeId, cancellationToken);
        if (code is null) return Result.Failure<long>(PublicMenuCodeErrors.NotFound(command.PublicMenuCodeId));
        if (code.Version != command.ExpectedVersion) return Result.Failure<long>(PublicMenuCodeErrors.VersionConflict(code.Id));
        code.Revoke(timeProvider.GetUtcNow());
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrencyException) { return Result.Failure<long>(PublicMenuCodeErrors.VersionConflict(code.Id)); }
        return Result.Success(code.Version);
    }
}
