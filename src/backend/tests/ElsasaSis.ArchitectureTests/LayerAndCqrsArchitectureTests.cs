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
        { AssemblyArchitectureContext.AcdModuleAssembly, "ElsasaSis.Modules.Acd" }
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
            AssemblyArchitectureContext.AcdModuleAssembly
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

    private static string FormatFailures(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        return $"Architecture rule violated by: {string.Join(", ", failingTypes)}";
    }
}
