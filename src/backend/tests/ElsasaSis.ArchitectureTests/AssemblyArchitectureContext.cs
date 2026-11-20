using System.Reflection;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Dev;
using ElsasaSis.Modules.Fin;
using ElsasaSis.Modules.Opr;
using ElsasaSis.Modules.Std;
using ElsasaSis.Modules.Tch;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.ArchitectureTests;

internal static class AssemblyArchitectureContext
{
    internal static readonly Assembly SharedKernelAssembly = typeof(Result).Assembly;
    internal static readonly Assembly ContractsAssembly = typeof(IOperationalContract).Assembly;
    internal static readonly Assembly AuthModuleAssembly = typeof(AuthAssemblyMarker).Assembly;
    internal static readonly Assembly OprModuleAssembly = typeof(OprAssemblyMarker).Assembly;
    internal static readonly Assembly StdModuleAssembly = typeof(StdAssemblyMarker).Assembly;
    internal static readonly Assembly TchModuleAssembly = typeof(TchAssemblyMarker).Assembly;
    internal static readonly Assembly AcdModuleAssembly = typeof(AcdAssemblyMarker).Assembly;
    internal static readonly Assembly FinModuleAssembly = typeof(FinAssemblyMarker).Assembly;
    internal static readonly Assembly DevModuleAssembly = typeof(DevAssemblyMarker).Assembly;
    internal static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    internal static readonly Assembly DatabaseAssembly = Assembly.Load("ElsasaSis.Database");

    internal static readonly Assembly[] AllApplicationAssemblies =
    [
        SharedKernelAssembly,
        ContractsAssembly,
        AuthModuleAssembly,
        OprModuleAssembly,
        StdModuleAssembly,
        TchModuleAssembly,
        AcdModuleAssembly,
        FinModuleAssembly,
        DevModuleAssembly,
        ApiAssembly,
        DatabaseAssembly
    ];
}
