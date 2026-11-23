using System.Data;
using System.Data.Common;
using M.Simple.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.EntityFrameworkCore.Test;

[TestClass]
public sealed class WriteOperationsTests
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TestContext Db => _scope.ServiceProvider.GetRequiredService<TestContext>();
    private IRepository<Product, long> Products => _scope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();
    private IUnitOfWork Work => _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    private SqliteOperationWriter Writer => new(_scope.ServiceProvider.GetRequiredService<ISimpleDbContext>());

    [TestInitialize]
    public async Task Initialize()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<TestContext>(o => o.UseSqlite(_connection));
        services.AddSimpleEfCore<TestContext>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        await Db.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task UpdateAndUpdateRange_RequireSaveAndReuseTrackedInstance()
    {
        var first = new Product { Name = "first", Price = 1 };
        var second = new Product { Name = "second", Price = 2 };
        Products.AddRange([first, second]);
        await Work.SaveChangesAsync();
        Products.Update(new Product { Id = first.Id, Name = "updated", Price = 3, Status = ProductStatus.Ready });
        Assert.AreEqual("first", (await Products.GetByIdAsync(first.Id))!.Name);
        Assert.AreEqual("updated", first.Name);
        Assert.AreEqual(2, Db.ChangeTracker.Entries().Count());
        Db.Entry(second).State = EntityState.Detached;
        Products.UpdateRange([new Product { Id = second.Id, Name = "range", Price = 4 }]);
        await Work.SaveChangesAsync();
        Assert.AreEqual("updated", (await Products.GetByIdAsync(first.Id))!.Name);
        Assert.AreEqual("range", (await Products.GetByIdAsync(second.Id))!.Name);
        Assert.AreEqual(ProductStatus.Ready, (await Products.GetByIdAsync(first.Id))!.Status);
        Assert.ThrowsException<ArgumentException>(() => Products.Update(new Product()));
        var unsaved = new Product { Name = "unsaved" };
        Products.Add(unsaved);
        Assert.ThrowsException<ArgumentException>(() => Products.Update(unsaved));
    }

    [TestMethod]
    public async Task ExecuteOperations_AreImmediateRespectPredicateAndParticipateInTransaction()
    {
        var first = new Product { Name = "keep", Enabled = true };
        Products.AddRange([first, new Product { Name = "remove" }]);
        await Work.SaveChangesAsync();
        await Work.BeginTransactionAsync();
        Assert.AreEqual(1, await Products.ExecuteUpdateAsync(p => p.Enabled, s => s.SetProperty(p => p.Name, "changed")));
        Assert.AreEqual("keep", first.Name); // Immediate SQL does not synchronize tracked entities.
        Assert.AreEqual("changed", (await Products.GetByIdAsync(first.Id))!.Name);
        Assert.AreEqual(1, await Products.ExecuteDeleteAsync(p => !p.Enabled));
        Assert.AreEqual(0, await Products.ExecuteDeleteAsync(p => p.Id < 0));
        await Work.RollbackTransactionAsync();
        Assert.AreEqual(2, await Products.CountAsync());
        Assert.AreEqual("keep", (await Products.GetByIdAsync(first.Id))!.Name);
    }

    [TestMethod]
    public async Task BulkUpdateAndDelete_PreserveUnselectedColumnsAndTracking()
    {
        var original = new Product { Name = "keep", Price = 10, Status = ProductStatus.Draft };
        Products.Add(original);
        await Work.SaveChangesAsync();
        Assert.AreEqual(1, await Writer.BulkUpdateAsync([new Product { Id = original.Id, Price = 30 }],
            new BulkUpdateOptions<Product> { Properties = [p => p.Price] }));
        Assert.AreEqual(10m, original.Price);
        Assert.AreEqual(EntityState.Unchanged, Db.Entry(original).State);
        var actual = (await Products.GetByIdAsync(original.Id))!;
        Assert.AreEqual(30m, actual.Price);
        Assert.AreEqual("keep", actual.Name);
        Assert.AreEqual(1, await Writer.BulkUpdateAsync([new Product { Id = original.Id, Name = "all", Status = ProductStatus.Ready }]));
        Assert.AreEqual(ProductStatus.Ready, (await Products.GetByIdAsync(original.Id))!.Status);
        Assert.AreEqual(0, await Writer.BulkDeleteAsync([new Product { Id = long.MaxValue }]));
        Assert.AreEqual(1, await Writer.BulkDeleteAsync([new Product { Id = original.Id }]));
        Assert.AreEqual(1, Db.ChangeTracker.Entries().Count());
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task GeneratedKeys_MapToInputAndOnlyMutateAfterAllChunksSucceed()
    {
        var rows = Enumerable.Range(0, 1001).Select(i => new Product { Name = "key_" + i }).ToArray();
        var failed = new SqliteOperationWriter(_scope.ServiceProvider.GetRequiredService<ISimpleDbContext>()) { FailOnBatch = 2 };
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => failed.BulkInsertAndReturnKeysAsync(rows));
        Assert.IsTrue(rows.All(p => p.Id == 0));
        Assert.AreEqual(0, await Products.CountAsync());
        var writer = Writer;
        await writer.BulkInsertAndReturnKeysAsync(rows);
        CollectionAssert.AreEqual(new[] { 1000, 1 }, writer.BatchSizes.ToArray());
        Assert.AreEqual(1001, rows.Select(p => p.Id).Distinct().Count());
        foreach (var row in new[] { rows[0], rows[500], rows[1000] })
            Assert.AreEqual(row.Name, (await Products.GetByIdAsync(row.Id))!.Name);
        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
    }

    [TestMethod]
    public async Task BulkOperations_UseCallerTransactionWithoutCommitting()
    {
        await Work.BeginTransactionAsync();
        var rows = new[] { new Product { Name = "original" } };
        await Writer.BulkInsertAndReturnKeysAsync(rows);
        Assert.IsTrue(rows[0].Id > 0);
        Assert.IsNotNull(Db.Database.CurrentTransaction);
        await Writer.BulkUpdateAsync([new Product { Id = rows[0].Id, Name = "changed" }]);
        await Writer.BulkDeleteAsync(rows);
        await Writer.BulkInsertAndReturnKeysAsync([new Product { Name = "pending" }]);
        await Work.RollbackTransactionAsync();
        Assert.AreEqual(0, await Products.CountAsync());
        Assert.IsTrue(rows[0].Id > 0); // Object values are not restored by database rollback.
    }

    [TestMethod]
    public async Task BulkValidation_RejectsUnsafeColumnsDuplicatesAndTrackedKeyInsert()
    {
        var rows = new[] { new Product { Id = 1 } };
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Writer.BulkUpdateAsync(rows,
            new BulkUpdateOptions<Product> { Properties = [p => p.Id] }));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Writer.BulkUpdateAsync(rows,
            new BulkUpdateOptions<Product> { Properties = [p => p.Name.Length] }));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Writer.BulkDeleteAsync([rows[0], rows[0]]));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Writer.BulkInsertAndReturnKeysAsync(rows));
        var pending = new Product { Name = "tracked" };
        Products.Add(pending);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Writer.BulkInsertAndReturnKeysAsync([pending]));
        Assert.AreEqual(EntityState.Added, Db.Entry(pending).State);
        Assert.IsNull(Db.Database.CurrentTransaction);
        Assert.AreEqual(0, await Products.CountAsync());
        Assert.AreEqual(0, await Writer.BulkDeleteAsync(Array.Empty<Product>()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { await Writer.BulkDeleteAsync(rows, cancellation.Token); Assert.Fail("Expected cancellation."); }
        catch (OperationCanceledException) { }
    }

    // Real SQLite exercises the shared pipeline; this is not a test of SQL Server/MySQL staging SQL.
    private sealed class SqliteOperationWriter(ISimpleDbContext accessor) : BulkWriterBase(accessor)
    {
        public int FailOnBatch { get; init; }
        public List<int> BatchSizes { get; } = [];
        protected override Task WriteBatchAsync(DataTable data, string table, string? schema, DbConnection connection,
            DbTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();
        protected override async Task<OperationResult> ExecuteBatchAsync(OperationBatch batch, DbConnection connection,
            DbTransaction transaction, CancellationToken cancellationToken)
        {
            BatchSizes.Add(batch.Data.Rows.Count);
            if (FailOnBatch == BatchSizes.Count) throw new InvalidOperationException("Injected failure.");
            var count = 0;
            var keys = new List<object>();
            var columns = batch.Data.Columns.Cast<DataColumn>().ToArray();
            static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
            foreach (DataRow row in batch.Data.Rows)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                foreach (var column in columns)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@p" + column.Ordinal;
                    parameter.Value = row[column];
                    command.Parameters.Add(parameter);
                }
                var table = Quote(batch.Table);
                command.CommandText = batch.Operation switch
                {
                    BulkOperation.Delete => $"DELETE FROM {table} WHERE {Quote(batch.KeyColumn)}=@p0",
                    BulkOperation.Update => $"UPDATE {table} SET {string.Join(",", columns.Skip(1).Select(c => Quote(c.ColumnName) + "=@p" + c.Ordinal))} WHERE {Quote(batch.KeyColumn)}=@p0",
                    _ => $"INSERT INTO {table} ({string.Join(",", columns.Select(c => Quote(c.ColumnName)))}) VALUES ({string.Join(",", columns.Select(c => "@p" + c.Ordinal))}) RETURNING {Quote(batch.KeyColumn)}"
                };
                if (batch.Operation == BulkOperation.InsertReturningKeys)
                {
                    keys.Add((await command.ExecuteScalarAsync(cancellationToken))!);
                    count++;
                }
                else count += await command.ExecuteNonQueryAsync(cancellationToken);
            }
            return new(count, keys);
        }
    }
}
