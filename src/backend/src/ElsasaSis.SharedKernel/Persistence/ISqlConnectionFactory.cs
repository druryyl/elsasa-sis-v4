using System.Data;

namespace ElsasaSis.SharedKernel.Persistence;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
