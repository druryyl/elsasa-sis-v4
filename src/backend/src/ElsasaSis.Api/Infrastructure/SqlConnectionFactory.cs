using System.Data;
using ElsasaSis.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Api.Infrastructure;

/// <summary>
/// Creates <see cref="SqlConnection"/> instances from <c>ConnectionStrings:DefaultConnection</c>.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection cannot be empty.");
        }
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
