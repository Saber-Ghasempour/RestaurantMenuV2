using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.RotatePublicMenuCode;
public sealed class RotatePublicMenuCodeCommandHandler(IPublicMenuCodeRepository codes,
    IUnitOfWork unitOfWork, IPublicMenuCodeGenerator generator, TimeProvider timeProvider)
    : ICommandHandler<RotatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>>
{
    public async Task<Result<IssuedPublicMenuCode>> Handle(RotatePublicMenuCodeCommand command, CancellationToken cancellationToken)
    {
        var code = await codes.GetByIdAsync(command.RestaurantId, command.PublicMenuCodeId, cancellationToken);
        if (code is null) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.NotFound(command.PublicMenuCodeId));
        if (code.Version != command.ExpectedVersion) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.VersionConflict(code.Id));
        string? raw = null; string? hash = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            raw = generator.Generate(); hash = generator.Hash(raw);
            if (!await codes.CodeHashExistsAsync(hash, cancellationToken)) break;
            raw = null;
        }
        if (raw is null) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.CollisionLimitExceeded);
        var rotated = code.Rotate(hash, timeProvider.GetUtcNow(), command.ExpiresAtUtc);
        if (rotated.IsFailure) return Result.Failure<IssuedPublicMenuCode>(rotated.Error);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PublicMenuCodeHashAlreadyExistsException) { return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.CollisionLimitExceeded); }
        catch (ConcurrencyException) { return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.VersionConflict(code.Id)); }
        return Result.Success(new IssuedPublicMenuCode(code.Id.Value, raw, code.Version));
    }
}
