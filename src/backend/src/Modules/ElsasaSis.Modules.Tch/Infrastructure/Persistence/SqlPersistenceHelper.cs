using System.Data;
using System.Data.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Infrastructure.Persistence;

internal static class SqlPersistenceHelper
{
    public static async Task ExecuteInTransactionAsync(
        ISqlConnectionFactory connectionFactory,
        Func<IDbConnection, IDbTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(connection, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                await action(connection, transaction, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public static bool IsUniqueConstraintViolation(DbException exception)
    {
        var numberProperty = exception.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(exception) is int number)
        {
            return number is 2601 or 2627;
        }

        return string.Equals(exception.SqlState, "23000", StringComparison.Ordinal);
    }

    public static bool MentionsConstraint(DbException exception, string constraintName) =>
        exception.Message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
}
