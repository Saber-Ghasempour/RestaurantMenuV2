using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Catalog.Application.Publications;

namespace RestaurantMenu.Catalog.Application.UnitTests.PublicMenus.GetPublicBranchMenu;

public sealed class GetPublicBranchMenuQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnCacheHitWithoutReadingDependencies()
    {
        var cached = new PublicBranchMenuResponse(
            Guid.CreateVersion7(), "Restaurant", Guid.CreateVersion7(),
            "Branch", [], null, null, null, null, null, null, null);
        var profile = new ProfileProviderStub(null);
        var reads = new ReadServiceStub();
        var handler = new GetPublicBranchMenuQueryHandler(
            profile, reads, new CacheStub(cached));

        var result = await handler.Handle(
            new GetPublicBranchMenuQuery(cached.RestaurantId, cached.BranchId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(cached, result.Value);
        Assert.Equal(0, profile.Calls);
        Assert.Equal(0, reads.Calls);
    }

    private sealed class ProfileProviderStub(BranchPublicProfile? profile)
        : IBranchPublicProfileProvider
    {
        public int Calls { get; private set; }

        public Task<BranchPublicProfile?> GetAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(profile);
        }
    }

    private sealed class ReadServiceStub : IBranchCatalogReadService
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<BranchCategoryPublicationResponse>> GetConfigurationAsync(
            Guid restaurantId, Guid branchId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BranchCategoryPublicationResponse>>([]);

        public Task<IReadOnlyList<PublicMenuCategoryResponse>> GetPublicMenuAsync(
            Guid restaurantId, Guid branchId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<PublicMenuCategoryResponse>>([]);
        }
    }

    private sealed class CacheStub(PublicBranchMenuResponse? value)
        : IPublicBranchMenuCache
    {
        public Task<PublicBranchMenuResponse?> GetAsync(Guid restaurantId,
            Guid branchId, CancellationToken cancellationToken) => Task.FromResult(value);

        public Task SetAsync(PublicBranchMenuResponse menu,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
