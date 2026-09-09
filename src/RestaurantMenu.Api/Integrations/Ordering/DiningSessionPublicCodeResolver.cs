using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ResolvePublicMenuCode;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Api.Integrations.Ordering;
public sealed class DiningSessionPublicCodeResolver(
    IQueryHandler<ResolvePublicMenuCodeQuery, Result<ResolvedPublicMenuCode>> handler)
    : IPublicCodeResolver
{
    public async Task<PublicCodeScope?> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(code), cancellationToken);
        if (result.IsFailure) return null;
        var value = result.Value;
        return new PublicCodeScope(value.RestaurantId, value.BranchId, value.DiningTableId,
            value.Purpose == RestaurantMenu.Restaurants.Domain.PublicMenuCodes.PublicMenuCodePurpose.DineInOrdering
                ? PublicCodePurpose.DineInOrdering : PublicCodePurpose.MenuOnly);
    }
}
