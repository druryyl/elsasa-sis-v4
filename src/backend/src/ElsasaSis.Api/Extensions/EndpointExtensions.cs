using System.Reflection;
using ElsasaSis.SharedKernel.Endpoints;

namespace ElsasaSis.Api.Extensions;

/// <summary>
/// Discovers and maps all <see cref="IEndpoint"/> implementations across module assemblies.
/// </summary>
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapDiscoveredEndpoints(
        this IEndpointRouteBuilder app,
        params Assembly[] assemblies)
    {
        var endpointTypes = assemblies
            .Distinct()
            .SelectMany(a => a.DefinedTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndpoint).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
