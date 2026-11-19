using System.Data;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Std;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Infrastructure.Contracts;
using ElsasaSis.Modules.Std.Infrastructure.Persistence;
using ElsasaSis.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Std;

public sealed class StdModuleTests
{
    [Fact]
    public void AddStdModule_RegistersRepositoriesAndStudentContract()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory, StubSqlConnectionFactory>();
        services.AddStdModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<CandidateRepository>(scope.ServiceProvider.GetRequiredService<ICandidateRepository>());
        Assert.IsType<StudentRepository>(scope.ServiceProvider.GetRequiredService<IStudentRepository>());
        Assert.IsType<StudentEnrollmentRepository>(scope.ServiceProvider.GetRequiredService<IStudentEnrollmentRepository>());
        Assert.IsType<StudyGroupRepository>(scope.ServiceProvider.GetRequiredService<IStudyGroupRepository>());
        Assert.IsType<StudentPlacementRepository>(scope.ServiceProvider.GetRequiredService<IStudentPlacementRepository>());
        Assert.IsType<StudentContract>(scope.ServiceProvider.GetRequiredService<IStudentContract>());
    }

    [Fact]
    public void StdDomainLayer_HasNoForbiddenDependencies()
    {
        var domainTypes = typeof(StdAssemblyMarker).Assembly
            .GetTypes()
            .Where(t => t.Namespace is not null && t.Namespace.Contains(".Domain", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(domainTypes);

        string[] forbiddenPrefixes =
        [
            "ElsasaSis.Modules.Std.Infrastructure",
            "ElsasaSis.Modules.Std.Features",
            "Dapper",
            "System.Data",
            "Microsoft.Data.SqlClient",
            "Microsoft.AspNetCore.Http"
        ];

        foreach (var type in domainTypes)
        {
            var referencedTypes = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)
                .Select(f => f.FieldType)
                .Concat(type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static).Select(p => p.PropertyType))
                .Concat(type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)
                    .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)));

            foreach (var referenced in referencedTypes)
            {
                var fullName = referenced.FullName ?? referenced.Name;
                Assert.False(
                    forbiddenPrefixes.Any(prefix => fullName.StartsWith(prefix, StringComparison.Ordinal)),
                    $"Domain type '{type.FullName}' references forbidden type '{fullName}'.");
            }
        }
    }

    private sealed class StubSqlConnectionFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => throw new NotSupportedException();

        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
