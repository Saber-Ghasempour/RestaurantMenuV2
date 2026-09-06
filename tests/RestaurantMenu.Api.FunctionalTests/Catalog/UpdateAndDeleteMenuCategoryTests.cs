using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class UpdateAndDeleteMenuCategoryTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateAndDeleteMenuCategoryTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task UpdateMenuCategoryShouldPersistChangesAndReturnVersion()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Update Category Restaurant",
            DateTimeOffset.UtcNow);
        var parent = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Parent",
            1);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Original",
            2);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}",
            new UpdateMenuCategoryRequest(
                parent.Id.Value,
                " Updated Category ",
                20,
                1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content
                .ReadFromJsonAsync<UpdateMenuCategoryResponse>();
        Assert.NotNull(content);
        Assert.Equal(category.Id.Value, content.Id);
        Assert.Equal(2, content.Version);

        var persisted = await _factory.FindMenuCategoryAsync(
            category.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal(parent.Id, persisted.ParentId);
        Assert.Equal("Updated Category", persisted.Name);
        Assert.Equal(20, persisted.DisplayOrder);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task UpdateMenuCategoryShouldReturnConflictForStaleVersion()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Stale Category Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Original",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}",
            new UpdateMenuCategoryRequest(
                null,
                "Updated",
                1,
                42));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.CategoryVersionConflict", problem.Title);
    }

    [Fact]
    public async Task UpdateMenuCategoryShouldRejectHierarchyCycle()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Cycle Category Restaurant",
            DateTimeOffset.UtcNow);
        var parent = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Parent",
            1);
        var child = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Child",
            2,
            parent.Id);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{parent.Id.Value}",
            new UpdateMenuCategoryRequest(
                child.Id.Value,
                "Parent",
                1,
                1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey(
                "Catalog.CategoryHierarchyCycle"));
    }

    [Fact]
    public async Task DeleteMenuCategoryShouldSoftDeleteCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Delete Category Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Delete Me",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(
            await _factory.FindMenuCategoryAsync(category.Id.Value));
        var persisted =
            await _factory.FindMenuCategoryIncludingDeletedAsync(
                category.Id.Value);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsDeleted);
        Assert.NotNull(persisted.DeletedAtUtc);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task DeleteMenuCategoryShouldReturnConflictWhenItHasChildren()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Protected Category Restaurant",
            DateTimeOffset.UtcNow);
        var parent = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Parent",
            1);
        await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Child",
            2,
            parent.Id);
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{parent.Id.Value}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.CategoryHasChildren", problem.Title);
        Assert.NotNull(
            await _factory.FindMenuCategoryAsync(parent.Id.Value));
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private sealed record UpdateMenuCategoryRequest(
        Guid? ParentCategoryId,
        string? Name,
        int DisplayOrder,
        long ExpectedVersion);

    private sealed record UpdateMenuCategoryResponse(
        Guid Id,
        long Version);

    private sealed record ProblemResponse(string Title);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}
