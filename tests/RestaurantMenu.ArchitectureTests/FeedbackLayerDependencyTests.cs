namespace RestaurantMenu.ArchitectureTests;
public sealed class FeedbackLayerDependencyTests
{
    private static readonly string[] Peers=["RestaurantMenu.Restaurants","RestaurantMenu.Catalog","RestaurantMenu.Media","RestaurantMenu.Ordering","RestaurantMenu.Notifications"];
    [Fact] public void DomainHasNoOuterOrPeerReferences()=>AssertNoReferences(typeof(RestaurantMenu.Feedback.Domain.AssemblyReference).Assembly,["RestaurantMenu.Feedback.Application","RestaurantMenu.Feedback.Infrastructure","RestaurantMenu.Feedback.Presentation","RestaurantMenu.Api",..Peers]);
    [Fact] public void ApplicationHasNoOuterOrPeerReferences()=>AssertNoReferences(typeof(RestaurantMenu.Feedback.Application.AssemblyReference).Assembly,["RestaurantMenu.Feedback.Infrastructure","RestaurantMenu.Feedback.Presentation","RestaurantMenu.Api",..Peers]);
    [Fact] public void InfrastructureHasNoPresentationApiOrPeerReferences()=>AssertNoReferences(typeof(RestaurantMenu.Feedback.Infrastructure.AssemblyReference).Assembly,["RestaurantMenu.Feedback.Presentation","RestaurantMenu.Api",..Peers]);
    [Fact] public void PresentationHasNoInfrastructureApiOrPeerReferences()=>AssertNoReferences(typeof(RestaurantMenu.Feedback.Presentation.AssemblyReference).Assembly,["RestaurantMenu.Feedback.Infrastructure","RestaurantMenu.Api",..Peers]);
    private static void AssertNoReferences(System.Reflection.Assembly source,IReadOnlyCollection<string> prefixes)
    { var violations=source.GetReferencedAssemblies().Select(x=>x.Name??string.Empty).Where(x=>prefixes.Any(p=>x.StartsWith(p,StringComparison.Ordinal))).ToArray(); Assert.Empty(violations); }
}
