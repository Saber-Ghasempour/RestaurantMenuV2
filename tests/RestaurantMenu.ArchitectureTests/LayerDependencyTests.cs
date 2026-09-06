using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ApiAssemblyReference = RestaurantMenu.Api.AssemblyReference;
using ApplicationAssemblyReference =
    RestaurantMenu.Restaurants.Application.AssemblyReference;
using DomainAssemblyReference =
    RestaurantMenu.Restaurants.Domain.AssemblyReference;
using InfrastructureAssemblyReference =
    RestaurantMenu.Restaurants.Infrastructure.AssemblyReference;
using PresentationAssemblyReference =
    RestaurantMenu.Restaurants.Presentation.AssemblyReference;
using ReflectionAssembly = System.Reflection.Assembly;
using SharedKernelAssemblyReference =
    RestaurantMenu.SharedKernel.AssemblyReference;

namespace RestaurantMenu.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly ReflectionAssembly ApiAssembly =
        typeof(ApiAssemblyReference).Assembly;

    private static readonly ReflectionAssembly SharedKernelAssembly =
        typeof(SharedKernelAssemblyReference).Assembly;

    private static readonly ReflectionAssembly DomainAssembly =
        typeof(DomainAssemblyReference).Assembly;

    private static readonly ReflectionAssembly ApplicationAssembly =
        typeof(ApplicationAssemblyReference).Assembly;

    private static readonly ReflectionAssembly InfrastructureAssembly =
        typeof(InfrastructureAssemblyReference).Assembly;

    private static readonly ReflectionAssembly PresentationAssembly =
        typeof(PresentationAssemblyReference).Assembly;

    private static readonly Architecture ArchitectureModel =
        new ArchLoader()
            .LoadAssemblies(
                ApiAssembly,
                SharedKernelAssembly,
                DomainAssembly,
                ApplicationAssembly,
                InfrastructureAssembly,
                PresentationAssembly)
            .Build();

    [Fact]
    public void SharedKernelShouldNotDependOnApi()
    {
        AssertDoesNotDependOn(
            SharedKernelAssembly,
            ApiAssembly);
    }

    [Fact]
    public void SharedKernelShouldNotDependOnRestaurantsModule()
    {
        AssertDoesNotDependOn(
            SharedKernelAssembly,
            DomainAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            ApplicationAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            PresentationAssembly);
    }

    [Fact]
    public void DomainShouldNotDependOnOuterLayers()
    {
        AssertDoesNotDependOn(
            DomainAssembly,
            ApplicationAssembly);

        AssertDoesNotDependOn(
            DomainAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            DomainAssembly,
            PresentationAssembly);

        AssertDoesNotDependOn(
            DomainAssembly,
            ApiAssembly);
    }

    [Fact]
    public void ApplicationShouldNotDependOnOuterLayers()
    {
        AssertDoesNotDependOn(
            ApplicationAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            ApplicationAssembly,
            PresentationAssembly);

        AssertDoesNotDependOn(
            ApplicationAssembly,
            ApiAssembly);
    }

    [Fact]
    public void InfrastructureShouldNotDependOnPresentationOrApi()
    {
        AssertDoesNotDependOn(
            InfrastructureAssembly,
            PresentationAssembly);

        AssertDoesNotDependOn(
            InfrastructureAssembly,
            ApiAssembly);
    }

    [Fact]
    public void PresentationShouldNotDependOnInfrastructureOrApi()
    {
        AssertDoesNotDependOn(
            PresentationAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            PresentationAssembly,
            ApiAssembly);
    }

    private static void AssertDoesNotDependOn(
    ReflectionAssembly sourceAssembly,
    ReflectionAssembly forbiddenAssembly)
    {
        var sourceTypes = Types()
            .That()
            .ResideInAssembly(sourceAssembly);

        var forbiddenTypes = Types()
            .That()
            .ResideInAssembly(forbiddenAssembly);

        sourceTypes
            .Should()
            .NotDependOnAny(forbiddenTypes)
            .Check(ArchitectureModel);
    }
}