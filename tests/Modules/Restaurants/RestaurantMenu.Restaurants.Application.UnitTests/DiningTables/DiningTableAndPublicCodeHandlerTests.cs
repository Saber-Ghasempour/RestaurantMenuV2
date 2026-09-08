using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Application.DiningTables.CreateDiningTable;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.CreatePublicMenuCode;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.DiningTables;

public sealed class DiningTableAndPublicCodeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTableShouldRejectInactiveBranch()
    {
        var branch = CreateBranch();
        branch.ChangeStatus(false);
        var tables = new TableRepositoryStub();
        var handler = new CreateDiningTableCommandHandler(new BranchRepositoryStub(branch),
            tables, new UnitOfWorkSpy(), new StubTimeProvider());

        var result = await handler.Handle(new CreateDiningTableCommand(
            branch.RestaurantId, branch.Id, 1, null, null), CancellationToken.None);

        Assert.Equal(DiningTableErrors.BranchInactive, result.Error);
        Assert.Null(tables.Added);
    }

    [Fact]
    public async Task CreateCodeShouldRetryHashCollisionAndReturnOnlyRawCode()
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Restaurant", Now).Value;
        var generator = new CodeGeneratorStub("colliding-secret", "unique-secret");
        var codes = new CodeRepositoryStub("collision-hash");
        var handler = new CreatePublicMenuCodeCommandHandler(
            new RestaurantRepositoryStub(restaurant), new BranchRepositoryStub(null),
            new TableRepositoryStub(), codes, new UnitOfWorkSpy(), generator,
            new StubTimeProvider());

        var result = await handler.Handle(new CreatePublicMenuCodeCommand(
            restaurant.Id, null, null, PublicMenuCodePurpose.MenuOnly, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("unique-secret", result.Value.Code);
        Assert.Equal("unique-hash", codes.Added!.CodeHash);
        Assert.DoesNotContain("unique-secret", codes.Added.CodeHash, StringComparison.Ordinal);
        Assert.Equal(2, generator.GenerateCalls);
    }

    private static Branch CreateBranch() => Branch.Create(BranchId.New(), RestaurantId.New(),
        "Branch", null, null, null, null, null, null, null, null, null, null, Now).Value;

    private sealed class StubTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class UnitOfWorkSpy : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
    private sealed class BranchRepositoryStub(Branch? branch) : IBranchRepository
    {
        public void Add(Branch value) => throw new NotSupportedException();
        public Task<Branch?> GetByIdAsync(RestaurantId restaurantId, BranchId branchId, CancellationToken cancellationToken) => Task.FromResult(branch);
    }
    private sealed class RestaurantRepositoryStub(Restaurant? restaurant) : IRestaurantRepository
    {
        public void Add(Restaurant value) => throw new NotSupportedException();
        public Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken) => Task.FromResult(restaurant);
    }
    private sealed class TableRepositoryStub(DiningTable? table = null) : IDiningTableRepository
    {
        public DiningTable? Added { get; private set; }
        public void Add(DiningTable value) => Added = value;
        public Task<DiningTable?> GetByIdAsync(RestaurantId restaurantId, BranchId branchId, DiningTableId id, CancellationToken cancellationToken) => Task.FromResult(table);
    }
    private sealed class CodeRepositoryStub(params string[] existingHashes) : IPublicMenuCodeRepository
    {
        public PublicMenuCode? Added { get; private set; }
        public void Add(PublicMenuCode value) => Added = value;
        public Task<PublicMenuCode?> GetByIdAsync(RestaurantId restaurantId, PublicMenuCodeId id, CancellationToken cancellationToken) => Task.FromResult<PublicMenuCode?>(null);
        public Task<bool> CodeHashExistsAsync(string codeHash, CancellationToken cancellationToken) => Task.FromResult(existingHashes.Contains(codeHash));
    }
    private sealed class CodeGeneratorStub(params string[] values) : IPublicMenuCodeGenerator
    {
        private int _index;
        public int GenerateCalls { get; private set; }
        public string Generate() { GenerateCalls++; return values[_index++]; }
        public string Hash(string code) => code == "colliding-secret" ? "collision-hash" : "unique-hash";
    }
}
