using System.Reflection;
using NetArchTest.Rules;

namespace ElsasaSis.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void AuthModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.AuthModuleAssembly,
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void OprModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.OprModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void StdModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.StdModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void TchModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.TchModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void AcdModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.AcdModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void FinModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.FinModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Dev",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void DevModule_ShouldNotDependOn_OtherModules_Api_Or_Database()
    {
        AssertModuleIsolation(
            AssemblyArchitectureContext.DevModuleAssembly,
            "ElsasaSis.Modules.Auth",
            "ElsasaSis.Modules.Opr",
            "ElsasaSis.Modules.Std",
            "ElsasaSis.Modules.Tch",
            "ElsasaSis.Modules.Acd",
            "ElsasaSis.Modules.Fin",
            "ElsasaSis.Api",
            "ElsasaSis.Database");
    }

    [Fact]
    public void SharedKernel_And_Contracts_ShouldNotDependOn_AnyModule_Api_Or_Database()
    {
        Assembly[] foundationAssemblies =
        [
            AssemblyArchitectureContext.SharedKernelAssembly,
            AssemblyArchitectureContext.ContractsAssembly
        ];

        var result = Types.InAssemblies(foundationAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ElsasaSis.Modules",
                "ElsasaSis.Api",
                "ElsasaSis.Database")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));

        foreach (var assembly in foundationAssemblies)
        {
            AssertNoAssemblyReference(
                assembly,
                "ElsasaSis.Modules.Auth",
                "ElsasaSis.Modules.Opr",
                "ElsasaSis.Modules.Std",
                "ElsasaSis.Modules.Tch",
                "ElsasaSis.Modules.Acd",
                "ElsasaSis.Modules.Fin",
                "ElsasaSis.Modules.Dev",
                "ElsasaSis.Api",
                "ElsasaSis.Database");
        }
    }

    [Fact]
    public void AllModules_ShouldOnlyReference_SharedKernel_And_Contracts_AmongInternalAssemblies()
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

        string[] allowedInternalAssemblies =
        [
            "ElsasaSis.SharedKernel",
            "ElsasaSis.Contracts"
        ];

        foreach (var assembly in moduleAssemblies)
        {
            var internalReferences = assembly.GetReferencedAssemblies()
                .Where(r => r.Name is not null && r.Name.StartsWith("ElsasaSis.", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Name!)
                .Where(name => !name.Equals(assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var disallowed = internalReferences
                .Where(name => !allowedInternalAssemblies.Contains(name, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            Assert.True(
                disallowed.Length == 0,
                $"Module '{assembly.GetName().Name}' references disallowed internal assemblies: {string.Join(", ", disallowed)}. Modules may only reference SharedKernel and Contracts.");
        }
    }

    [Fact]
    public void NoApplicationAssembly_ShouldReference_EntityFrameworkCore()
    {
        var result = Types.InAssemblies(AssemblyArchitectureContext.AllApplicationAssemblies)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));

        foreach (var assembly in AssemblyArchitectureContext.AllApplicationAssemblies)
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

    private static void AssertModuleIsolation(Assembly moduleAssembly, params string[] forbiddenDependencies)
    {
        var result = Types.InAssembly(moduleAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
        AssertNoAssemblyReference(moduleAssembly, forbiddenDependencies);
    }

    private static void AssertNoAssemblyReference(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var referencedNames = assembly.GetReferencedAssemblies()
            .Select(r => r.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var violations = forbiddenAssemblyNames
            .Where(referencedNames.Contains)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Assembly '{assembly.GetName().Name}' has forbidden assembly references: {string.Join(", ", violations)}");
    }

    private static string FormatFailures(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        return $"Architecture rule violated by: {string.Join(", ", failingTypes)}";
    }
}
