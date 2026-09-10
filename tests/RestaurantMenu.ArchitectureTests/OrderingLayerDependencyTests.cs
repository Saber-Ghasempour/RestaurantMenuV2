namespace RestaurantMenu.ArchitectureTests;

public sealed class OrderingLayerDependencyTests
{
    private static readonly string[] PeerPrefixes =
        ["RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Media"];

    [Fact]
    public void DomainShouldNotReferenceOuterLayersOrPeerModules()
    {
        AssertNoReferences(typeof(RestaurantMenu.Ordering.Domain.AssemblyReference).Assembly,
            ["RestaurantMenu.Ordering.Application", "RestaurantMenu.Ordering.Infrastructure",
             "RestaurantMenu.Ordering.Presentation", "RestaurantMenu.Notifications",
             "RestaurantMenu.Api", .. PeerPrefixes]);
        foreach (var peer in PeerAssemblies)
            AssertNoReferences(peer, ["RestaurantMenu.Ordering"]);
    }

    [Fact]
    public void ApplicationShouldNotReferenceOuterLayersOrPeerModules() => AssertNoReferences(
        typeof(RestaurantMenu.Ordering.Application.AssemblyReference).Assembly,
        ["RestaurantMenu.Ordering.Infrastructure", "RestaurantMenu.Ordering.Presentation",
         "RestaurantMenu.Notifications", "RestaurantMenu.Api", .. PeerPrefixes]);

    [Fact]
    public void InfrastructureShouldNotReferencePresentationApiOrPeerModules() => AssertNoReferences(
        typeof(RestaurantMenu.Ordering.Infrastructure.AssemblyReference).Assembly,
        ["RestaurantMenu.Ordering.Presentation", "RestaurantMenu.Notifications",
         "RestaurantMenu.Api", .. PeerPrefixes]);

    [Fact]
    public void PresentationShouldNotReferenceInfrastructureApiOrPeerModules() => AssertNoReferences(
        typeof(RestaurantMenu.Ordering.Presentation.AssemblyReference).Assembly,
        ["RestaurantMenu.Ordering.Infrastructure", "RestaurantMenu.Notifications",
         "RestaurantMenu.Api", .. PeerPrefixes]);

    private static void AssertNoReferences(System.Reflection.Assembly source,
        IReadOnlyCollection<string> prefixes)
    {
        var violations = source.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty)
            .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
        Assert.Empty(violations);
    }

    private static readonly System.Reflection.Assembly[] PeerAssemblies =
    [
        typeof(RestaurantMenu.Restaurants.Domain.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Restaurants.Application.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Restaurants.Infrastructure.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Restaurants.Presentation.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Catalog.Domain.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Catalog.Application.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Catalog.Infrastructure.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Catalog.Presentation.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Media.Domain.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Media.Application.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Media.Infrastructure.AssemblyReference).Assembly,
        typeof(RestaurantMenu.Media.Presentation.AssemblyReference).Assembly
    ];
}
