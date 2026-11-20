using System.Data;
using System.Data.Common;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

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

    public static async Task<TResult> ExecuteResultInTransactionAsync<TResult>(
        ISqlConnectionFactory connectionFactory,
        Func<IDbConnection, IDbTransaction, CancellationToken, Task<TResult>> action,
        Func<DbException, TResult?> uniqueConstraintMapper,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action(connection, transaction, cancellationToken);
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return result;
            }
            catch (DbException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                var mapped = uniqueConstraintMapper(ex);
                if (mapped is not null)
                {
                    return mapped;
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        using var syncTransaction = connection.BeginTransaction();
        try
        {
            var result = await action(connection, syncTransaction, cancellationToken);
            if (result.IsSuccess)
            {
                syncTransaction.Commit();
            }
            else
            {
                syncTransaction.Rollback();
            }

            return result;
        }
        catch (DbException ex) when (IsUniqueConstraintViolation(ex))
        {
            syncTransaction.Rollback();
            var mapped = uniqueConstraintMapper(ex);
            if (mapped is not null)
            {
                return mapped;
            }

            throw;
        }
        catch
        {
            syncTransaction.Rollback();
            throw;
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

    public static DateTime SpecifyUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
