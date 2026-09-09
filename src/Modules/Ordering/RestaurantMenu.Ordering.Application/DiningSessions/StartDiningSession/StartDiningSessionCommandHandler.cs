using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.DiningSessions.StartDiningSession;

public sealed class StartDiningSessionCommandHandler(IPublicCodeResolver publicCodes,
    IDiningSessionRepository sessions, IOrderingUnitOfWork unitOfWork,
    IDiningSessionTokenGenerator tokenGenerator, TimeProvider timeProvider,
    DiningSessionOptions options)
    : ICommandHandler<StartDiningSessionCommand, Result<IssuedDiningSession>>
{
    public async Task<Result<IssuedDiningSession>> Handle(StartDiningSessionCommand command,
        CancellationToken cancellationToken)
    {
        var publicCode = await publicCodes.ResolveAsync(command.Code, cancellationToken);
        if (publicCode is not { Purpose: PublicCodePurpose.DineInOrdering,
            BranchId: not null, DiningTableId: not null })
            return Result.Failure<IssuedDiningSession>(DiningSessionErrors.InvalidPublicCode);

        var token = await GenerateUniqueTokenAsync(cancellationToken);
        if (token is null)
            return Result.Failure<IssuedDiningSession>(DiningSessionErrors.TokenCollisionLimitExceeded);
        var now = timeProvider.GetUtcNow();
        var created = DiningSession.Create(DiningSessionId.New(), token.Value.Hash,
            publicCode.RestaurantId, publicCode.BranchId.Value, publicCode.DiningTableId.Value,
            now, now.Add(options.Lifetime));
        if (created.IsFailure) return Result.Failure<IssuedDiningSession>(created.Error);
        sessions.Add(created.Value);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DiningSessionTokenHashAlreadyExistsException)
        { return Result.Failure<IssuedDiningSession>(DiningSessionErrors.TokenCollisionLimitExceeded); }
        return Result.Success(new IssuedDiningSession(created.Value.Id.Value, token.Value.Raw,
            created.Value.RestaurantId, created.Value.BranchId, created.Value.DiningTableId,
            created.Value.ExpiresAtUtc));
    }

    private async Task<(string Raw, string Hash)?> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var raw = tokenGenerator.Generate(); var hash = tokenGenerator.Hash(raw);
            if (!await sessions.TokenHashExistsAsync(hash, cancellationToken)) return (raw, hash);
        }
        return null;
    }
}
