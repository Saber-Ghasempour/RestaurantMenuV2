using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ApiAssemblyReference = RestaurantMenu.Api.AssemblyReference;
using ApplicationAbstractionsAssemblyReference =
    RestaurantMenu.Application.Abstractions.AssemblyReference;
using ApplicationAssemblyReference =
    RestaurantMenu.Restaurants.Application.AssemblyReference;
using CatalogApplicationAssemblyReference =
    RestaurantMenu.Catalog.Application.AssemblyReference;
using CatalogDomainAssemblyReference =
    RestaurantMenu.Catalog.Domain.AssemblyReference;
using CatalogInfrastructureAssemblyReference =
    RestaurantMenu.Catalog.Infrastructure.AssemblyReference;
using CatalogPresentationAssemblyReference =
    RestaurantMenu.Catalog.Presentation.AssemblyReference;
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

    private static readonly ReflectionAssembly CatalogDomainAssembly =
        typeof(CatalogDomainAssemblyReference).Assembly;

    private static readonly ReflectionAssembly CatalogApplicationAssembly =
        typeof(CatalogApplicationAssemblyReference).Assembly;

    private static readonly ReflectionAssembly CatalogInfrastructureAssembly =
        typeof(CatalogInfrastructureAssemblyReference).Assembly;

    private static readonly ReflectionAssembly CatalogPresentationAssembly =
        typeof(CatalogPresentationAssemblyReference).Assembly;

    private static readonly ReflectionAssembly ApplicationAssembly =
        typeof(ApplicationAssemblyReference).Assembly;

    private static readonly ReflectionAssembly InfrastructureAssembly =
        typeof(InfrastructureAssemblyReference).Assembly;

    private static readonly ReflectionAssembly PresentationAssembly =
        typeof(PresentationAssemblyReference).Assembly;

    private static readonly ReflectionAssembly ApplicationAbstractionsAssembly =
        typeof(ApplicationAbstractionsAssemblyReference).Assembly;

    private static readonly Architecture ArchitectureModel =
        new ArchLoader()
            .LoadAssemblies(
                ApiAssembly,
                SharedKernelAssembly,
                ApplicationAbstractionsAssembly,
                CatalogDomainAssembly,
                CatalogApplicationAssembly,
                CatalogInfrastructureAssembly,
                CatalogPresentationAssembly,
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

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            CatalogDomainAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            CatalogApplicationAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            CatalogInfrastructureAssembly);

        AssertDoesNotDependOn(
            SharedKernelAssembly,
            CatalogPresentationAssembly);
    }

    [Fact]
    public void CatalogDomainShouldNotDependOnRestaurantsModuleOrApi()
    {
        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            DomainAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            ApplicationAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            PresentationAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            ApiAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            CatalogApplicationAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            CatalogInfrastructureAssembly);

        AssertDoesNotDependOn(
            CatalogDomainAssembly,
            CatalogPresentationAssembly);
    }

    [Fact]
    public void CatalogApplicationShouldNotDependOnOuterLayersOrRestaurants()
    {
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            CatalogInfrastructureAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            CatalogPresentationAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            ApiAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            DomainAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            ApplicationAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            InfrastructureAssembly);
        AssertDoesNotDependOn(
            CatalogApplicationAssembly,
            PresentationAssembly);
    }

    [Fact]
    public void CatalogInfrastructureShouldNotDependOnPresentationApiOrRestaurants()
    {
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            CatalogPresentationAssembly);
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            ApiAssembly);
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            DomainAssembly);
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            ApplicationAssembly);
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            InfrastructureAssembly);
        AssertDoesNotDependOn(
            CatalogInfrastructureAssembly,
            PresentationAssembly);
    }

    [Fact]
    public void CatalogPresentationShouldNotDependOnInfrastructureApiOrRestaurants()
    {
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
            CatalogInfrastructureAssembly);
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
            ApiAssembly);
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
            DomainAssembly);
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
            ApplicationAssembly);
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
            InfrastructureAssembly);
        AssertDoesNotDependOn(
            CatalogPresentationAssembly,
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

    [Fact]
    public void SharedKernelShouldNotDependOnApplicationAbstractions()
    {
        AssertDoesNotDependOn(
            SharedKernelAssembly,
            ApplicationAbstractionsAssembly);
    }

    [Fact]
    public void ApplicationAbstractionsShouldNotDependOnModulesOrApi()
    {
        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            DomainAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            ApplicationAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            InfrastructureAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            PresentationAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            ApiAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            CatalogDomainAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            CatalogApplicationAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            CatalogInfrastructureAssembly);

        AssertDoesNotDependOn(
            ApplicationAbstractionsAssembly,
            CatalogPresentationAssembly);
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
