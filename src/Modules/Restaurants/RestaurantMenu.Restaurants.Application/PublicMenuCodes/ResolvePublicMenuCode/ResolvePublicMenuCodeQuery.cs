using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.ResolvePublicMenuCode;
public sealed record ResolvePublicMenuCodeQuery(string Code) : IQuery<Result<ResolvedPublicMenuCode>>;
public sealed class ResolvePublicMenuCodeQueryHandler(IPublicMenuCodeReadService readService,
    IPublicMenuCodeGenerator generator, TimeProvider timeProvider)
    : IQueryHandler<ResolvePublicMenuCodeQuery, Result<ResolvedPublicMenuCode>>
{
    public async Task<Result<ResolvedPublicMenuCode>> Handle(ResolvePublicMenuCodeQuery query, CancellationToken cancellationToken)
    {
        if (query.Code.Length != 43 || query.Code.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            return Result.Failure<ResolvedPublicMenuCode>(Domain.PublicMenuCodes.PublicMenuCodeErrors.InvalidOrExpired);
        var resolved = await readService.ResolveAsync(generator.Hash(query.Code), timeProvider.GetUtcNow(), cancellationToken);
        return resolved is null
            ? Result.Failure<ResolvedPublicMenuCode>(Domain.PublicMenuCodes.PublicMenuCodeErrors.InvalidOrExpired)
            : Result.Success(resolved);
    }
}
