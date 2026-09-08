using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.Catalog.Application.Publications.GetBranchCatalogConfiguration;
using RestaurantMenu.Catalog.Application.Publications.SetBranchCategoryPublications;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Publications;

public static class BranchCategoryPublicationEndpoints
{
    public static IEndpointRouteBuilder MapBranchCategoryPublicationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(
                "/api/restaurants/{restaurantId:guid}/branches/{branchId:guid}/category-publications")
            .WithTags("Catalog Publications");

        group.MapGet("/", GetAsync)
            .WithName("GetBranchCatalogConfiguration")
            .Produces<IReadOnlyList<BranchCategoryPublicationResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);
        group.MapPut("/", SetAsync)
            .WithName("SetBranchCategoryPublications")
            .Produces<SetBranchCategoryPublicationsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid restaurantId,
        Guid branchId,
        IQueryHandler<GetBranchCatalogConfigurationQuery,
            Result<IReadOnlyList<BranchCategoryPublicationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetBranchCatalogConfigurationQuery(restaurantId, branchId),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> SetAsync(
        Guid restaurantId,
        Guid branchId,
        SetBranchCategoryPublicationsRequest request,
        ICommandHandler<SetBranchCategoryPublicationsCommand, Result<bool>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await handler.Handle(
            new SetBranchCategoryPublicationsCommand(
                restaurantId,
                branchId,
                (request.Publications ?? []).Select(publication =>
                    new BranchCategoryPublicationInput(
                        publication.CategoryId,
                        publication.IsPublished,
                        publication.DisplayOrderOverride)).ToArray()),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new SetBranchCategoryPublicationsResponse(result.Value))
            : result.Error.ToProblem();
    }
}

public sealed record SetBranchCategoryPublicationsRequest(
    IReadOnlyList<BranchCategoryPublicationRequest>? Publications);

public sealed record BranchCategoryPublicationRequest(
    Guid CategoryId,
    bool IsPublished,
    int? DisplayOrderOverride);

public sealed record SetBranchCategoryPublicationsResponse(bool Changed);
