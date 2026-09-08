using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.ListPublicMenuCodes;
public sealed record ListPublicMenuCodesQuery(RestaurantId RestaurantId)
    : IQuery<Result<IReadOnlyList<PublicMenuCodeResponse>>>;
public sealed class ListPublicMenuCodesQueryHandler(IPublicMenuCodeReadService readService,
    IRestaurantRepository restaurants)
    : IQueryHandler<ListPublicMenuCodesQuery, Result<IReadOnlyList<PublicMenuCodeResponse>>>
{
    public async Task<Result<IReadOnlyList<PublicMenuCodeResponse>>> Handle(ListPublicMenuCodesQuery query, CancellationToken cancellationToken)
    {
        if (await restaurants.GetByIdAsync(query.RestaurantId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<PublicMenuCodeResponse>>(RestaurantErrors.NotFound(query.RestaurantId));
        return Result.Success(await readService.ListAsync(query.RestaurantId, cancellationToken));
    }
}
