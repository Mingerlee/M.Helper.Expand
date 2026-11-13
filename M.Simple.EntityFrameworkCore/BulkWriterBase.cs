using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace M.Simple.EntityFrameworkCore.Infrastructure;

/// <summary>Shared bounded-memory pipeline for provider adapters. Supports simple single-table entities.</summary>
public abstract class BulkWriterBase(ISimpleDbContext accessor) : IBulkWriter
{
    protected DbContext Context => accessor.Context;

    public async Task BulkInsertAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        cancellationToken.ThrowIfCancellationRequested();
        var plan = CreatePlan<TEntity>();
        using var batches = entities.Chunk(1000).GetEnumerator();
        if (!batches.MoveNext()) return;
        var connection = Context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        IDbContextTransaction? ownedTransaction = null;
        try
        {
            if (wasClosed) await Context.Database.OpenConnectionAsync(cancellationToken);
            if (Context.Database.CurrentTransaction is null)
                ownedTransaction = await Context.Database.BeginTransactionAsync(cancellationToken);
            var transaction = Context.Database.CurrentTransaction!.GetDbTransaction();
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var data = new DataTable();
                foreach (var property in plan.Properties)
                {
                    var converter = property.GetTypeMapping().Converter;
                    var type = converter?.ProviderClrType ?? property.ClrType;
                    type = Nullable.GetUnderlyingType(type) ?? type;
                    data.Columns.Add(property.GetColumnName(plan.Store)!, type);
                }
                foreach (var entity in batches.Current)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ArgumentNullException.ThrowIfNull(entity);
                    var values = plan.Properties.Select(property =>
                    {
                        var value = property.PropertyInfo!.GetValue(entity);
                        var converter = property.GetTypeMapping().Converter;
                        if (converter is not null) value = converter.ConvertToProvider(value);
                        return value ?? DBNull.Value;
                    }).ToArray();
                    data.Rows.Add(values);
                }
                await WriteBatchAsync(data, plan.Table, plan.Schema, connection, transaction, cancellationToken);
            } while (batches.MoveNext());
            if (ownedTransaction is not null) await ownedTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // Disposing an uncommitted owned transaction rolls back all chunks.
            if (ownedTransaction is not null) await ownedTransaction.DisposeAsync();
            if (wasClosed) await Context.Database.CloseConnectionAsync();
        }
    }

    protected abstract Task WriteBatchAsync(DataTable data, string table, string? schema,
        DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken);

    private BulkPlan CreatePlan<TEntity>() where TEntity : class
    {
        var entity = Context.Model.FindEntityType(typeof(TEntity))
            ?? throw new NotSupportedException("Entity is not mapped in the context.");
        var table = entity.GetTableName();
        var schema = entity.GetSchema();
        if (table is null || entity.IsOwned() || entity.BaseType is not null || entity.GetDerivedTypes().Any()
            || entity.GetTableMappings().Count() != 1
            || Context.Model.GetEntityTypes().Any(other => other != entity && other.GetTableName() == table && other.GetSchema() == schema))
            throw new NotSupportedException("Bulk insert requires a single unshared table without inheritance or owned entities.");
        var store = StoreObjectIdentifier.Table(table, schema);
        var properties = entity.GetProperties().Where(p => p.ValueGenerated == ValueGenerated.Never).ToArray();
        if (properties.Length == 0 || properties.Any(p => p.PropertyInfo?.GetMethod is null || p.GetColumnName(store) is null))
            throw new NotSupportedException("Bulk insert requires readable mapped scalar properties; shadow and field-only columns are unsupported.");
        // Native copy cannot run EF client-side GUID/string key generators.
        if (entity.FindPrimaryKey() is null || entity.GetProperties().Any(p => p.IsPrimaryKey()
            && p.ValueGenerated != ValueGenerated.Never && p.ClrType != typeof(int) && p.ClrType != typeof(long) && p.ClrType != typeof(short)))
            throw new NotSupportedException("Use assigned keys (ValueGeneratedNever) or numeric database-generated keys for bulk insert.");
        return new(table, schema, store, properties);
    }

    private sealed record BulkPlan(string Table, string? Schema, StoreObjectIdentifier Store, IProperty[] Properties);
}
