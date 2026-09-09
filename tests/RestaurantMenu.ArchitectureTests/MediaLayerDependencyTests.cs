namespace RestaurantMenu.ArchitectureTests;

public sealed class MediaLayerDependencyTests
{
    private static readonly string[] OuterOrPeerPrefixes =
    [
        "RestaurantMenu.Media.Application", "RestaurantMenu.Media.Infrastructure",
        "RestaurantMenu.Media.Presentation", "RestaurantMenu.Restaurants", "RestaurantMenu.Catalog",
        "RestaurantMenu.Api"
    ];

    [Fact]
    public void MediaDomainShouldNotReferenceOuterLayersOrPeerModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Media.Domain.AssemblyReference).Assembly,
            OuterOrPeerPrefixes);

    [Fact]
    public void MediaApplicationShouldNotReferenceInfrastructurePresentationOrPeerModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Media.Application.AssemblyReference).Assembly,
            ["RestaurantMenu.Media.Infrastructure", "RestaurantMenu.Media.Presentation",
             "RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Api"]);

    [Fact]
    public void MediaInfrastructureShouldNotReferencePresentationApiOrPeerModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Media.Infrastructure.AssemblyReference).Assembly,
            ["RestaurantMenu.Media.Presentation", "RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Api"]);

    [Fact]
    public void MediaPresentationShouldNotReferenceInfrastructureApiOrPeerModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Media.Presentation.AssemblyReference).Assembly,
            ["RestaurantMenu.Media.Infrastructure", "RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Api"]);

    private static void AssertNoReferences(System.Reflection.Assembly source, IReadOnlyCollection<string> prefixes)
    {
        var violations = source.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty)
            .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
        Assert.Empty(violations);
    }
}
