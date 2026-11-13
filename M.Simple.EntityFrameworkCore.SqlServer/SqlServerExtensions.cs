using System.Data;
using System.Data.Common;
using M.Simple.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.EntityFrameworkCore.SqlServer;

public static class SqlServerExtensions
{
    public static IServiceCollection AddSimpleEfCoreSqlServer<TContext>(this IServiceCollection services,
        string connectionString, Action<DbContextOptionsBuilder>? configure = null) where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddSimpleEfCore<TContext>();
        services.AddDbContext<TContext>(options =>
        {
            options.UseSqlServer(connectionString);
            configure?.Invoke(options);
        });
        services.AddScoped<IBulkWriter, SqlServerBulkWriter>();
        return services;
    }
}

internal sealed class SqlServerBulkWriter(ISimpleDbContext accessor) : BulkWriterBase(accessor)
{
    protected override async Task WriteBatchAsync(DataTable data, string table, string? schema,
        DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        static string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";
        using var copy = new SqlBulkCopy((SqlConnection)connection,
            SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.KeepNulls,
            (SqlTransaction)transaction)
        {
            DestinationTableName = schema is null ? Quote(table) : Quote(schema) + "." + Quote(table),
            BatchSize = data.Rows.Count,
            BulkCopyTimeout = Context.Database.GetCommandTimeout() ?? 30
        };
        foreach (DataColumn column in data.Columns) copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        await copy.WriteToServerAsync(data, cancellationToken);
    }
}
