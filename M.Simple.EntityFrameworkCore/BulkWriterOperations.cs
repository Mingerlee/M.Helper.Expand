using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace M.Simple.EntityFrameworkCore.Infrastructure;

public abstract partial class BulkWriterBase
{
    public Task<int> BulkUpdateAsync<TEntity>(IEnumerable<TEntity> entities, BulkUpdateOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default) where TEntity : class
        => ExecuteOperationAsync(entities, BulkOperation.Update, options, cancellationToken);

    public Task<int> BulkDeleteAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        => ExecuteOperationAsync(entities, BulkOperation.Delete, null, cancellationToken);

    public async Task BulkInsertAndReturnKeysAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        => await ExecuteOperationAsync(entities, BulkOperation.InsertReturningKeys, null, cancellationToken).ConfigureAwait(false);

    protected enum BulkOperation { Update, Delete, InsertReturningKeys }
    protected sealed record OperationBatch(BulkOperation Operation, DataTable Data, string Table, string? Schema,
        string KeyColumn, string KeyStoreType);
    protected sealed record OperationResult(int AffectedRows, IReadOnlyList<object>? Keys = null);

    // Virtual defaults preserve existing custom BulkWriterBase implementations that only support insert.
    protected virtual Task<OperationResult> ExecuteBatchAsync(OperationBatch batch, DbConnection connection,
        DbTransaction transaction, CancellationToken cancellationToken)
        => throw new NotSupportedException("This bulk adapter does not implement update, delete or generated-key insert.");

    protected virtual void ValidateGeneratedKey(IProperty key) { }

    private async Task<int> ExecuteOperationAsync<TEntity>(IEnumerable<TEntity> entities, BulkOperation operation,
        BulkUpdateOptions<TEntity>? options, CancellationToken ct) where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ct.ThrowIfCancellationRequested();
        if (System.Transactions.Transaction.Current is not null)
            throw new NotSupportedException("Ambient transactions are unsupported; use an explicit EF transaction.");
        var (entityType, store, key, properties) = CreateOperationPlan(operation, options);
        using var chunks = entities.Chunk(1000).GetEnumerator();
        if (!chunks.MoveNext()) return 0;
        var keyColumn = key.GetColumnName(store)!;
        var connection = Context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        IDbContextTransaction? owned = null;
        var returned = new List<(TEntity Entity, object Key)>();
        var inputKeys = new HashSet<object>();
        var inputObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var affected = 0;
        try
        {
            if (wasClosed) await Context.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            if (Context.Database.CurrentTransaction is null)
                owned = await Context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var transaction = Context.Database.CurrentTransaction!.GetDbTransaction();
            do
            {
                ct.ThrowIfCancellationRequested();
                using var data = new DataTable();
                foreach (var property in properties)
                {
                    var type = property.GetTypeMapping().Converter?.ProviderClrType ?? property.ClrType;
                    type = Nullable.GetUnderlyingType(type) ?? type;
                    data.Columns.Add(property.GetColumnName(store)!, type);
                }
                foreach (var entity in chunks.Current)
                {
                    ArgumentNullException.ThrowIfNull(entity);
                    ct.ThrowIfCancellationRequested();
                    var value = key.PropertyInfo!.GetValue(entity);
                    if (operation == BulkOperation.InsertReturningKeys)
                    {
                        if (Context.Entry(entity).State != EntityState.Detached || !inputObjects.Add(entity))
                            throw new ArgumentException("Generated-key insert requires distinct detached entity instances.", nameof(entities));
                        if (value is not null && !value.Equals(Activator.CreateInstance(key.ClrType)))
                            throw new ArgumentException("Generated-key insert requires default key values.", nameof(entities));
                    }
                    else
                    {
                        if (value is null || (key.ValueGenerated != ValueGenerated.Never && value.Equals(Activator.CreateInstance(key.ClrType))))
                            throw new ArgumentException("Bulk update/delete requires persisted non-null keys.", nameof(entities));
                        var convertedKey = key.GetTypeMapping().Converter?.ConvertToProvider(value) ?? value;
                        if (!inputKeys.Add(convertedKey))
                            throw new ArgumentException("Duplicate bulk keys are unsupported.", nameof(entities));
                    }
                    data.Rows.Add(properties.Select(property =>
                    {
                        var item = property.PropertyInfo!.GetValue(entity);
                        var converter = property.GetTypeMapping().Converter;
                        return (converter is null ? item : converter.ConvertToProvider(item)) ?? DBNull.Value;
                    }).ToArray());
                }
                var result = await ExecuteBatchAsync(new(operation, data, entityType.GetTableName()!, entityType.GetSchema(),
                    keyColumn, key.GetColumnType()), connection, transaction, ct).ConfigureAwait(false);
                affected = checked(affected + result.AffectedRows);
                if (operation == BulkOperation.InsertReturningKeys)
                {
                    if (result.Keys?.Count != chunks.Current.Length || result.AffectedRows != chunks.Current.Length)
                        throw new InvalidOperationException("Generated-key result count does not match input.");
                    for (var i = 0; i < chunks.Current.Length; i++)
                    {
                        var value = result.Keys[i];
                        var converter = key.GetTypeMapping().Converter;
                        var modelKey = converter is null ? Convert.ChangeType(value, key.ClrType) : converter.ConvertFromProvider(value);
                        returned.Add((chunks.Current[i], modelKey!));
                    }
                }
            } while (chunks.MoveNext());
            if (owned is not null) await owned.CommitAsync(ct).ConfigureAwait(false);
            // Delay key mutation until every batch succeeds; an external rollback does not undo object values.
            foreach (var item in returned) key.PropertyInfo!.SetValue(item.Entity, item.Key);
            return affected;
        }
        finally
        {
            try { if (owned is not null) await owned.DisposeAsync().ConfigureAwait(false); }
            finally { if (wasClosed) await Context.Database.CloseConnectionAsync().ConfigureAwait(false); }
        }
    }

    private (IEntityType Entity, StoreObjectIdentifier Store, IProperty Key, IProperty[] Properties)
        CreateOperationPlan<TEntity>(BulkOperation operation, BulkUpdateOptions<TEntity>? options) where TEntity : class
    {
        var entity = Context.Model.FindEntityType(typeof(TEntity)) ?? throw new NotSupportedException("Entity is not mapped.");
        var table = entity.GetTableName();
        var schema = entity.GetSchema();
        if (table is null || entity.IsOwned() || entity.BaseType is not null || entity.GetDerivedTypes().Any()
            || entity.GetTableMappings().Count() != 1 || Context.Model.GetEntityTypes().Any(other => other != entity
                && other.GetTableName() == table && other.GetSchema() == schema))
            throw new NotSupportedException("Bulk operations require a single unshared table without inheritance or owned entities.");
        var key = entity.FindPrimaryKey();
        if (key?.Properties.Count != 1 || key.Properties[0].PropertyInfo?.GetMethod is null)
            throw new NotSupportedException("Bulk update/delete/key-return requires a single CLR primary key.");
        var primaryKey = key.Properties[0];
        var store = StoreObjectIdentifier.Table(table, schema);
        IProperty[] properties;
        if (operation == BulkOperation.InsertReturningKeys)
        {
            if (primaryKey.ValueGenerated != ValueGenerated.OnAdd || primaryKey.PropertyInfo!.SetMethod is null
                || primaryKey.GetTypeMapping().Converter is not null
                || (primaryKey.ClrType != typeof(int) && primaryKey.ClrType != typeof(long) && primaryKey.ClrType != typeof(short)))
                throw new NotSupportedException("Key-return requires a writable numeric database-generated key without a converter.");
            ValidateGeneratedKey(primaryKey);
            properties = entity.GetProperties().Where(p => p.ValueGenerated == ValueGenerated.Never).ToArray();
        }
        else if (operation == BulkOperation.Delete) properties = [primaryKey];
        else
        {
            bool Writable(IProperty p) => !p.IsKey() && !p.IsConcurrencyToken && p.ValueGenerated == ValueGenerated.Never
                && p.GetAfterSaveBehavior() == PropertySaveBehavior.Save;
            if (options?.Properties is null && options is not null) throw new ArgumentException("Properties cannot be null.", nameof(options));
            var requested = options?.Properties ?? [];
            var updates = requested.Length == 0 ? entity.GetProperties().Where(Writable).ToArray()
                : requested.Select(selector =>
                {
                    ArgumentNullException.ThrowIfNull(selector);
                    Expression body = selector.Body;
                    if (body is UnaryExpression { NodeType: ExpressionType.Convert } convert) body = convert.Operand;
                    if (body is not MemberExpression { Member: PropertyInfo } member || member.Expression != selector.Parameters[0])
                        throw new ArgumentException("Select a direct scalar property.", nameof(options));
                    var property = entity.FindProperty(member.Member.Name);
                    if (property is null || !Writable(property)) throw new ArgumentException("Keys, generated and concurrency columns cannot be updated.", nameof(options));
                    return property;
                }).Distinct().ToArray();
            if (updates.Length == 0) throw new ArgumentException("No writable update columns.", nameof(options));
            properties = [primaryKey, .. updates];
        }
        if (properties.Length == 0 || properties.Any(p => p.PropertyInfo?.GetMethod is null || p.GetColumnName(store) is null))
            throw new NotSupportedException("Bulk operations require readable mapped CLR scalar properties.");
        return (entity, store, primaryKey, properties);
    }
}
