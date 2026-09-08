using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.CreatePublicMenuCode;
public sealed class CreatePublicMenuCodeCommandHandler(IRestaurantRepository restaurants,
    IBranchRepository branches, IDiningTableRepository tables, IPublicMenuCodeRepository codes,
    IUnitOfWork unitOfWork, IPublicMenuCodeGenerator generator, TimeProvider timeProvider)
    : ICommandHandler<CreatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>>
{
    public async Task<Result<IssuedPublicMenuCode>> Handle(CreatePublicMenuCodeCommand command, CancellationToken cancellationToken)
    {
        if (await restaurants.GetByIdAsync(command.RestaurantId, cancellationToken) is null)
            return Result.Failure<IssuedPublicMenuCode>(RestaurantErrors.NotFound(command.RestaurantId));
        if (command.BranchId is { } branchId)
        {
            var branch = await branches.GetByIdAsync(command.RestaurantId, branchId, cancellationToken);
            if (branch is null) return Result.Failure<IssuedPublicMenuCode>(BranchErrors.NotFound(branchId));
            if (!branch.IsActive) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.BranchInactive);
        }
        if (command.DiningTableId is { } tableId && command.BranchId is { } tableBranchId)
        {
            var table = await tables.GetByIdAsync(command.RestaurantId, tableBranchId, tableId, cancellationToken);
            if (table is null) return Result.Failure<IssuedPublicMenuCode>(DiningTableErrors.NotFound(tableId));
            if (!table.IsActive) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.TableInactive);
        }
        var now = timeProvider.GetUtcNow();
        var candidate = await GenerateUniqueAsync(cancellationToken);
        if (candidate is null) return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.CollisionLimitExceeded);
        var created = PublicMenuCode.Create(PublicMenuCodeId.New(), candidate.Value.Hash,
            command.RestaurantId, command.BranchId, command.DiningTableId, command.Purpose,
            command.ExpiresAtUtc, now);
        if (created.IsFailure) return Result.Failure<IssuedPublicMenuCode>(created.Error);
        codes.Add(created.Value);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PublicMenuCodeHashAlreadyExistsException) { return Result.Failure<IssuedPublicMenuCode>(PublicMenuCodeErrors.CollisionLimitExceeded); }
        return Result.Success(new IssuedPublicMenuCode(created.Value.Id.Value, candidate.Value.Raw, created.Value.Version));
    }

    private async Task<(string Raw, string Hash)?> GenerateUniqueAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var raw = generator.Generate();
            var hash = generator.Hash(raw);
            if (!await codes.CodeHashExistsAsync(hash, cancellationToken)) return (raw, hash);
        }
        return null;
    }
}
