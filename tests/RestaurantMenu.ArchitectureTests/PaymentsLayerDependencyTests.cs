namespace RestaurantMenu.ArchitectureTests;

public sealed class PaymentsLayerDependencyTests
{
    private static readonly string[] Peers =
    [
        "RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Media",
        "RestaurantMenu.Ordering", "RestaurantMenu.Notifications", "RestaurantMenu.Feedback"
    ];

    [Fact]
    public void DomainHasNoOuterOrPeerReferences() => AssertNoReferences(
        typeof(RestaurantMenu.Payments.Domain.AssemblyReference).Assembly,
        ["RestaurantMenu.Payments.Application", "RestaurantMenu.Payments.Infrastructure", "RestaurantMenu.Payments.Presentation", "RestaurantMenu.Api", .. Peers]);

    [Fact]
    public void ApplicationHasNoOuterOrPeerReferences() => AssertNoReferences(
        typeof(RestaurantMenu.Payments.Application.AssemblyReference).Assembly,
        ["RestaurantMenu.Payments.Infrastructure", "RestaurantMenu.Payments.Presentation", "RestaurantMenu.Api", .. Peers]);

    [Fact]
    public void InfrastructureHasNoPresentationApiOrPeerReferences() => AssertNoReferences(
        typeof(RestaurantMenu.Payments.Infrastructure.AssemblyReference).Assembly,
        ["RestaurantMenu.Payments.Presentation", "RestaurantMenu.Api", .. Peers]);

    [Fact]
    public void PresentationHasNoInfrastructureApiOrPeerReferences() => AssertNoReferences(
        typeof(RestaurantMenu.Payments.Presentation.AssemblyReference).Assembly,
        ["RestaurantMenu.Payments.Infrastructure", "RestaurantMenu.Api", .. Peers]);

    private static void AssertNoReferences(System.Reflection.Assembly source,
        IReadOnlyCollection<string> prefixes)
    {
        var violations = source.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty)
            .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
        Assert.Empty(violations);
    }
}
