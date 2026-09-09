using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;

public sealed record ResolveDiningSessionQuery(string Token, Guid? ExpectedRestaurantId = null,
    Guid? ExpectedBranchId = null, Guid? ExpectedDiningTableId = null)
    : IQuery<Result<DiningSessionScope>>;

public sealed class ResolveDiningSessionQueryHandler(IDiningSessionRepository sessions,
    IDiningSessionTokenGenerator tokenGenerator, TimeProvider timeProvider)
    : IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>>
{
    public async Task<Result<DiningSessionScope>> Handle(ResolveDiningSessionQuery query,
        CancellationToken cancellationToken)
    {
        if (!tokenGenerator.IsWellFormed(query.Token))
            return Result.Failure<DiningSessionScope>(DiningSessionErrors.InvalidCapability);
        var scope = await sessions.ResolveAsync(tokenGenerator.Hash(query.Token),
            timeProvider.GetUtcNow(), cancellationToken);
        if (scope is null ||
            query.ExpectedRestaurantId is { } restaurantId && scope.RestaurantId != restaurantId ||
            query.ExpectedBranchId is { } branchId && scope.BranchId != branchId ||
            query.ExpectedDiningTableId is { } tableId && scope.DiningTableId != tableId)
            return Result.Failure<DiningSessionScope>(DiningSessionErrors.InvalidCapability);
        return Result.Success(scope);
    }
}
