using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ApiAssemblyReference = RestaurantMenu.Api.AssemblyReference;
using SharedKernelAssemblyReference = RestaurantMenu.SharedKernel.AssemblyReference;

namespace RestaurantMenu.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Architecture ArchitectureModel = new ArchLoader()
        .LoadAssemblies(
            typeof(ApiAssemblyReference).Assembly,
            typeof(SharedKernelAssemblyReference).Assembly)
        .Build();

    [Fact]
    public void SharedKernelShouldNotDependOnApi()
    {
        var sharedKernelTypes = Types()
            .That()
            .ResideInAssembly(typeof(SharedKernelAssemblyReference).Assembly);

        var apiTypes = Types()
            .That()
            .ResideInAssembly(typeof(ApiAssemblyReference).Assembly);

        sharedKernelTypes
            .Should()
            .NotDependOnAny(apiTypes)
            .Check(ArchitectureModel);
    }
}