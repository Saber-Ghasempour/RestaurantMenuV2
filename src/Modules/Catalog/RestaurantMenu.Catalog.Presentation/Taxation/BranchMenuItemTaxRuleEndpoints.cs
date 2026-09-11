using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Taxation;
using RestaurantMenu.Catalog.Domain.Taxation;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Taxation;

public static class BranchMenuItemTaxRuleEndpoints
{
    public static IEndpointRouteBuilder MapBranchMenuItemTaxRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(
            "/api/restaurants/{restaurantId:guid}/branches/{branchId:guid}/item-tax-rules")
            .WithTags("Catalog Taxation");
        group.MapGet("/", GetAsync).WithName("ListBranchMenuItemTaxRules")
            .Produces<IReadOnlyList<BranchMenuItemTaxRuleResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);
        group.MapPut("/{menuItemId:guid}", PutAsync).WithName("SetBranchMenuItemTaxRule")
            .Produces<BranchMenuItemTaxRuleResponse>().ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid restaurantId, Guid branchId,
        IQueryHandler<ListBranchMenuItemTaxRulesQuery,
            Result<IReadOnlyList<BranchMenuItemTaxRuleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(restaurantId, branchId), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> PutAsync(Guid restaurantId, Guid branchId,
        Guid menuItemId, SetBranchMenuItemTaxRuleRequest request,
        ICommandHandler<SetBranchMenuItemTaxRuleCommand,
            Result<BranchMenuItemTaxRuleResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(restaurantId, branchId, menuItemId,
            request.RateBasisPoints, request.Behavior, request.ExpectedVersion), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
    }
}

public sealed record SetBranchMenuItemTaxRuleRequest(int RateBasisPoints,
    TaxBehavior Behavior, long ExpectedVersion);
