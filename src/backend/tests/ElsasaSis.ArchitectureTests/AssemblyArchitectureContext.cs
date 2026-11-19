using System.Reflection;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Opr;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.ArchitectureTests;

internal static class AssemblyArchitectureContext
{
    internal static readonly Assembly SharedKernelAssembly = typeof(Result).Assembly;
    internal static readonly Assembly ContractsAssembly = typeof(IOperationalContract).Assembly;
    internal static readonly Assembly AuthModuleAssembly = typeof(AuthAssemblyMarker).Assembly;
    internal static readonly Assembly OprModuleAssembly = typeof(OprAssemblyMarker).Assembly;
    internal static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    internal static readonly Assembly DatabaseAssembly = Assembly.Load("ElsasaSis.Database");

    internal static readonly Assembly[] AllApplicationAssemblies =
    [
        SharedKernelAssembly,
        ContractsAssembly,
        AuthModuleAssembly,
        OprModuleAssembly,
        ApiAssembly,
        DatabaseAssembly
    ];
}
