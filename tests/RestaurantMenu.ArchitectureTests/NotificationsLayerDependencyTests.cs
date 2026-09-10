namespace RestaurantMenu.ArchitectureTests;

public sealed class NotificationsLayerDependencyTests
{
    private static readonly string[] OtherBusinessModules =
        ["RestaurantMenu.Restaurants", "RestaurantMenu.Catalog", "RestaurantMenu.Media"];

    [Fact]
    public void ApplicationShouldDependOnlyOnOrderingContracts() => AssertNoReferences(
        typeof(RestaurantMenu.Notifications.Application.AssemblyReference).Assembly,
        ["RestaurantMenu.Notifications.Infrastructure", "RestaurantMenu.Notifications.Presentation",
         "RestaurantMenu.Ordering.Domain", "RestaurantMenu.Ordering.Infrastructure",
         "RestaurantMenu.Ordering.Presentation", "RestaurantMenu.Api", .. OtherBusinessModules]);

    [Fact]
    public void InfrastructureShouldNotReferencePresentationApiOrOtherBusinessModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Notifications.Infrastructure.AssemblyReference).Assembly,
            ["RestaurantMenu.Notifications.Presentation", "RestaurantMenu.Api", .. OtherBusinessModules]);

    [Fact]
    public void PresentationShouldNotReferenceInfrastructureApiOrOtherBusinessModules() =>
        AssertNoReferences(typeof(RestaurantMenu.Notifications.Presentation.AssemblyReference).Assembly,
            ["RestaurantMenu.Notifications.Infrastructure", "RestaurantMenu.Api", .. OtherBusinessModules]);

    private static void AssertNoReferences(System.Reflection.Assembly source,
        IReadOnlyCollection<string> prefixes)
    {
        var violations = source.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty)
            .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();
        Assert.Empty(violations);
    }
}
