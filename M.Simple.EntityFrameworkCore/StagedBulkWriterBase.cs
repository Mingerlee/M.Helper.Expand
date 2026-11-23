using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace M.Simple.EntityFrameworkCore.Infrastructure;

/// <summary>Native staging-table update/delete and generated-key inserts for SQL Server/MySQL adapters.</summary>
public abstract class StagedBulkWriterBase(ISimpleDbContext accessor) : BulkWriterBase(accessor)
{
    protected abstract bool IsMySql { get; }
    private string Quote(string identifier) => IsMySql ? "`" + identifier.Replace("`", "``") + "`" : "[" + identifier.Replace("]", "]]") + "]";

    protected override async Task<OperationResult> ExecuteBatchAsync(OperationBatch batch, DbConnection connection,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var target = batch.Schema is null ? Quote(batch.Table) : Quote(batch.Schema) + "." + Quote(batch.Table);
        if (IsMySql && batch.Operation == BulkOperation.InsertReturningKeys)
            return await InsertMySqlKeysAsync(batch, target, connection, transaction, cancellationToken).ConfigureAwait(false);
        var tempName = (IsMySql ? "msimple_" : "#msimple_") + Guid.NewGuid().ToString("N");
        var temp = Quote(tempName);
        var columns = batch.Data.Columns.Cast<DataColumn>().Select(c => Quote(c.ColumnName)).ToArray();
        var columnList = string.Join(",", columns);
        var ordinalName = "ordinal_" + Guid.NewGuid().ToString("N");
        var ordinal = Quote(ordinalName);
        var key = Quote(batch.KeyColumn);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = Context.Database.GetCommandTimeout() ?? 30;
        async Task<int> Execute(string sql, CancellationToken ct)
        {
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        await Execute(IsMySql
            ? $"CREATE TEMPORARY TABLE {temp} AS SELECT {columnList} FROM {target} WHERE 1=0"
            // A join prevents SQL Server from copying the IDENTITY attribute to the staging table.
            : $"SELECT TOP (0) {string.Join(",", columns.Select(c => c == key ? $"CAST(t.{c} AS {batch.KeyStoreType}) AS {c}" : "t." + c))} INTO {temp} FROM {target} t CROSS JOIN (SELECT 1 AS dummy) x", cancellationToken).ConfigureAwait(false);
        Exception? original = null;
        try
        {
            if (batch.Operation == BulkOperation.InsertReturningKeys)
            {
                await Execute($"ALTER TABLE {temp} ADD {ordinal} int NOT NULL", cancellationToken).ConfigureAwait(false);
                batch.Data.Columns.Add(ordinalName, typeof(int));
                for (var i = 0; i < batch.Data.Rows.Count; i++) batch.Data.Rows[i][ordinalName] = i;
            }
            else if (!IsMySql)
                await Execute($"CREATE UNIQUE INDEX {Quote("ix_" + Guid.NewGuid().ToString("N"))} ON {temp} ({key})", cancellationToken).ConfigureAwait(false);
            await WriteBatchAsync(batch.Data, tempName, null, connection, transaction, cancellationToken).ConfigureAwait(false);
            if (batch.Operation == BulkOperation.InsertReturningKeys)
            {
                command.CommandText = $"DECLARE @result TABLE (ordinal int NOT NULL, value {batch.KeyStoreType} NOT NULL); "
                    + $"MERGE INTO {target} AS t USING {temp} AS s ON 1=0 WHEN NOT MATCHED THEN INSERT ({columnList}) VALUES ({string.Join(",", columns.Select(c => "s." + c))}) "
                    + $"OUTPUT s.{ordinal}, inserted.{key} INTO @result; SELECT ordinal,value FROM @result ORDER BY ordinal;";
                var keys = new object[batch.Data.Rows.Count];
                var seen = new bool[keys.Length];
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var index = reader.GetInt32(0);
                    if (index < 0 || index >= keys.Length || seen[index]) throw new InvalidOperationException("Invalid generated-key ordinal.");
                    seen[index] = true;
                    keys[index] = reader.GetValue(1);
                }
                if (seen.Any(value => !value)) throw new InvalidOperationException("Missing generated keys.");
                return new(keys.Length, keys);
            }
            if (IsMySql)
            {
                // ALTER/CREATE INDEX on a MySQL temporary table can implicitly commit the caller's transaction.
                // Check staging duplicates with a read instead of adding an index after creation.
                command.CommandText = $"SELECT COUNT(*) FROM (SELECT {key} FROM {temp} GROUP BY {key} HAVING COUNT(*) > 1) duplicate_keys";
                if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0)
                    throw new ArgumentException("Bulk keys must be unique under the database collation.");
            }
            var match = $"t.{key}=s.{key}";
            var sql = batch.Operation == BulkOperation.Delete
                ? $"DELETE t FROM {target} t INNER JOIN {temp} s ON {match}"
                : $"UPDATE t SET {string.Join(",", columns.Where(c => c != key).Select(c => (IsMySql ? "t." : "") + c + "=s." + c))} FROM {target} t INNER JOIN {temp} s ON {match}";
            if (IsMySql && batch.Operation == BulkOperation.Update)
                sql = $"UPDATE {target} t INNER JOIN {temp} s ON {match} SET {string.Join(",", columns.Where(c => c != key).Select(c => "t." + c + "=s." + c))}";
            if (!IsMySql)
            {
                command.CommandText = sql + "; SELECT @@ROWCOUNT;";
                return new(Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)));
            }
            var affected = await Execute(sql, cancellationToken).ConfigureAwait(false);
            command.CommandText = "SHOW WARNINGS";
            await using (var warnings = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                if (await warnings.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("MySQL bulk operation reported a warning; roll back the transaction.");
            return new(affected);
        }
        catch (Exception ex) { original = ex; throw; }
        finally
        {
            // Temporary tables are connection-scoped; always remove them, even in a caller-owned transaction.
            try { await Execute(IsMySql ? $"DROP TEMPORARY TABLE IF EXISTS {temp}" : $"DROP TABLE IF EXISTS {temp}", CancellationToken.None).ConfigureAwait(false); }
            catch (Exception cleanup) when (original is not null) { original.Data["BulkCleanupException"] = cleanup; }
        }
    }

    private async Task<OperationResult> InsertMySqlKeysAsync(OperationBatch batch, string target,
        DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = Context.Database.GetCommandTimeout() ?? 30;
        var columns = batch.Data.Columns.Cast<DataColumn>().ToArray();
        var sql = $"INSERT INTO {target} ({string.Join(",", columns.Select(c => Quote(c.ColumnName)))}) VALUES ({string.Join(",", columns.Select(c => "@p" + c.Ordinal))}); SELECT LAST_INSERT_ID();";
        foreach (var column in columns)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p" + column.Ordinal;
            command.Parameters.Add(parameter);
        }
        var keys = new List<object>();
        foreach (DataRow row in batch.Data.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var column in columns) command.Parameters[column.Ordinal].Value = row[column];
            command.CommandText = sql;
            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (value is null || value is DBNull || Convert.ToInt64(value) <= 0)
                throw new InvalidOperationException("MySQL did not return a positive generated identity.");
            keys.Add(value);
            command.CommandText = "SHOW WARNINGS";
            await using var warnings = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await warnings.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("MySQL generated-key insert reported a warning; roll back the transaction.");
        }
        return new(keys.Count, keys);
    }
}
