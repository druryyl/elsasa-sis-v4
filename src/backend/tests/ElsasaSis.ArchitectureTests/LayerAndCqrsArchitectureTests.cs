using System.Reflection;
using ElsasaSis.SharedKernel.Persistence;
using NetArchTest.Rules;

namespace ElsasaSis.ArchitectureTests;

public sealed class LayerAndCqrsArchitectureTests
{
    public static TheoryData<Assembly, string> ModuleAssemblies => new()
    {
        { AssemblyArchitectureContext.AuthModuleAssembly, "ElsasaSis.Modules.Auth" },
        { AssemblyArchitectureContext.OprModuleAssembly, "ElsasaSis.Modules.Opr" },
        { AssemblyArchitectureContext.StdModuleAssembly, "ElsasaSis.Modules.Std" },
        { AssemblyArchitectureContext.TchModuleAssembly, "ElsasaSis.Modules.Tch" },
        { AssemblyArchitectureContext.AcdModuleAssembly, "ElsasaSis.Modules.Acd" },
        { AssemblyArchitectureContext.FinModuleAssembly, "ElsasaSis.Modules.Fin" },
        { AssemblyArchitectureContext.DevModuleAssembly, "ElsasaSis.Modules.Dev" }
    };

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void DomainLayer_ShouldNotDependOn_Infrastructure_Features_DataAccess_Or_Http(
        Assembly moduleAssembly,
        string moduleRootNamespace)
    {
        var domainTypes = Types.InAssembly(moduleAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain");

        Assert.NotEmpty(domainTypes.GetTypes());

        var result = domainTypes
            .ShouldNot()
            .HaveDependencyOnAny(
                $"{moduleRootNamespace}.Infrastructure",
                $"{moduleRootNamespace}.Features",
                "Dapper",
                "System.Data",
                "Microsoft.Data.SqlClient",
                "Microsoft.AspNetCore.Http")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void WriteSliceHandlers_ShouldNotDependOn_SqlConnectionFactory_Or_Dapper()
    {
        Assembly[] moduleAssemblies =
        [
            AssemblyArchitectureContext.AuthModuleAssembly,
            AssemblyArchitectureContext.OprModuleAssembly,
            AssemblyArchitectureContext.StdModuleAssembly,
            AssemblyArchitectureContext.TchModuleAssembly,
            AssemblyArchitectureContext.AcdModuleAssembly,
            AssemblyArchitectureContext.FinModuleAssembly,
            AssemblyArchitectureContext.DevModuleAssembly
        ];

        var writeHandlers = Types.InAssemblies(moduleAssemblies)
            .That()
            .ResideInNamespaceContaining(".Features")
            .And()
            .HaveNameEndingWith("Handler")
            .And()
            .DoNotHaveNameStartingWith("Get");

        Assert.NotEmpty(writeHandlers.GetTypes());

        var result = writeHandlers
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dapper",
                typeof(ISqlConnectionFactory).FullName!)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void AllModuleAssemblies_ShouldNotReference_EntityFrameworkCore()
    {
        Assembly[] moduleAssemblies =
        [
            AssemblyArchitectureContext.AuthModuleAssembly,
            AssemblyArchitectureContext.OprModuleAssembly,
            AssemblyArchitectureContext.StdModuleAssembly,
            AssemblyArchitectureContext.TchModuleAssembly,
            AssemblyArchitectureContext.AcdModuleAssembly,
            AssemblyArchitectureContext.FinModuleAssembly,
            AssemblyArchitectureContext.DevModuleAssembly
        ];

        var result = Types.InAssemblies(moduleAssemblies)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));

        foreach (var assembly in moduleAssemblies)
        {
            var efRefs = assembly.GetReferencedAssemblies()
                .Where(r => r.Name is not null &&
                            r.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Name!)
                .ToArray();

            Assert.True(
                efRefs.Length == 0,
                $"Assembly '{assembly.GetName().Name}' must not reference EntityFrameworkCore, found: {string.Join(", ", efRefs)}");
        }
    }

    private static string FormatFailures(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        return $"Architecture rule violated by: {string.Join(", ", failingTypes)}";
    }
}
