using System.Data;
using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Infrastructure.Persistence;

public sealed class ReceiptSequenceGenerator(ISqlConnectionFactory connectionFactory) : IReceiptSequenceGenerator
{
    private const string AtomicIncrementSql = """
        UPDATE fin.receipt_sequences
           SET next_seq = next_seq + 1
        OUTPUT deleted.next_seq
         WHERE month = @Month;
        """;

    private const string SafeInsertSql = """
        BEGIN TRY
            INSERT INTO fin.receipt_sequences (month, next_seq)
            VALUES (@Month, 2);
            SELECT 1;
        END TRY
        BEGIN CATCH
            UPDATE fin.receipt_sequences
               SET next_seq = next_seq + 1
            OUTPUT deleted.next_seq
             WHERE month = @Month;
        END CATCH
        """;

    public Task<string> GenerateNextReceiptNumberAsync(
        string month,
        CancellationToken cancellationToken = default) =>
        GenerateNextReceiptNumberAsync(month, null, cancellationToken);

    public async Task<string> GenerateNextReceiptNumberAsync(
        string month,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var (dbMonth, cleanMonth) = NormalizeMonth(month);

        int seq;
        if (transaction is not null)
        {
            seq = await ExecuteSequenceRetrievalAsync(transaction.Connection!, transaction, dbMonth, cancellationToken);
        }
        else
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            seq = await ExecuteSequenceRetrievalAsync(connection, null, dbMonth, cancellationToken);
        }

        return $"KWT-{cleanMonth}-{seq:D4}";
    }

    private static async Task<int> ExecuteSequenceRetrievalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string dbMonth,
        CancellationToken cancellationToken)
    {
        var nextSeq = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                AtomicIncrementSql,
                new { Month = dbMonth },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (nextSeq.HasValue)
        {
            return nextSeq.Value;
        }

        // Row did not exist; insert 2 and return 1 (or catch concurrent insert)
        var fallbackSeq = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                SafeInsertSql,
                new { Month = dbMonth },
                transaction: transaction,
                cancellationToken: cancellationToken));

        return fallbackSeq ?? 1;
    }

    private static (string DbMonth, string CleanMonth) NormalizeMonth(string month)
    {
        var trimmed = month.Trim();
        if (trimmed.Contains('-'))
        {
            var clean = trimmed.Replace("-", string.Empty);
            return (trimmed, clean);
        }

        if (trimmed.Length == 6)
        {
            var db = $"{trimmed.Substring(0, 4)}-{trimmed.Substring(4, 2)}";
            return (db, trimmed);
        }

        return (trimmed, trimmed.Replace("-", string.Empty));
    }
}
