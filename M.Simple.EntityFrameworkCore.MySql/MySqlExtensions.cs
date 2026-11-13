using System.Data;
using System.Data.Common;
using M.Simple.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace M.Simple.EntityFrameworkCore.MySql;

public static class MySqlExtensions
{
    public static IServiceCollection AddSimpleEfCoreMySql<TContext>(this IServiceCollection services,
        string connectionString, ServerVersion serverVersion, Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(serverVersion);
        services.AddSimpleEfCore<TContext>();
        services.AddDbContext<TContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion);
            configure?.Invoke(options);
        });
        services.AddScoped<IBulkWriter, MySqlBulkWriter>();
        return services;
    }
}

internal sealed class MySqlBulkWriter(ISimpleDbContext accessor) : BulkWriterBase(accessor)
{
    protected override async Task WriteBatchAsync(DataTable data, string table, string? schema,
        DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        static string Quote(string identifier) => "`" + identifier.Replace("`", "``") + "`";
        var copy = new MySqlBulkCopy((MySqlConnection)connection, (MySqlTransaction)transaction)
        {
            DestinationTableName = schema is null ? Quote(table) : Quote(schema) + "." + Quote(table),
            BulkCopyTimeout = Context.Database.GetCommandTimeout() ?? 30
        };
        foreach (DataColumn column in data.Columns)
            copy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(column.Ordinal, column.ColumnName));
        var result = await copy.WriteToServerAsync(data, cancellationToken);
        if (result.Warnings.Count > 0 || result.RowsInserted != data.Rows.Count)
            throw new InvalidOperationException("MySQL bulk copy reported warnings or a row count mismatch; transaction must be rolled back.");
    }
}
