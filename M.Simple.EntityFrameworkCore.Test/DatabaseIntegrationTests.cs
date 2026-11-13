using System.Diagnostics;
using M.Simple.EntityFrameworkCore.MySql;
using M.Simple.EntityFrameworkCore.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;

namespace M.Simple.EntityFrameworkCore.Test;

/// <summary>Opt-in tests create and drop ONLY a uniquely named disposable database.</summary>
[TestClass]
public sealed class DatabaseIntegrationTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DataRow("sqlserver", "MSIMPLE_EF_SQLSERVER")]
    [DataRow("mysql", "MSIMPLE_EF_MYSQL")]
    public async Task NativeProviderCrudBulkAndRollback(string database, string variable)
    {
        var configured = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(configured)) Assert.Inconclusive($"Set {variable} to opt in to disposable-database tests.");
        var name = "msimple_ef_test_" + Guid.NewGuid().ToString("N");
        string connection;
        if (database == "sqlserver")
        {
            var builder = new SqlConnectionStringBuilder(configured) { InitialCatalog = name };
            connection = builder.ConnectionString;
        }
        else
        {
            var builder = new MySqlConnectionStringBuilder(configured) { Database = name, AllowLoadLocalInfile = true };
            connection = builder.ConnectionString;
        }
        var services = new ServiceCollection();
        if (database == "sqlserver") services.AddSimpleEfCoreSqlServer<TestContext>(connection);
        else services.AddSimpleEfCoreMySql<TestContext>(connection, ServerVersion.AutoDetect(configured));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var writer = scope.ServiceProvider.GetRequiredService<IBulkWriter>();
            var product = new Product { Name = "original", Price = 10 };
            repository.Add(product);
            await work.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.AreEqual("original", (await repository.GetByIdAsync(product.Id))!.Name);
            repository.UpdateProperties(new Product { Id = product.Id, Price = 20 }, x => x.Price);
            await work.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.AreEqual("original", (await repository.GetByIdAsync(product.Id))!.Name);
            Assert.AreEqual(20m, (await repository.GetByIdAsync(product.Id))!.Price);
            var paged = await repository.Query().OrderBy(x => x.Id).Select(x => x.Name).ToPageAsync();
            Assert.AreEqual(1, paged.TotalCount);
            Assert.IsTrue(await repository.RemoveByIdAsync(product.Id));
            await work.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var rows = Enumerable.Range(0, 10000).Select(i => new Product
            {
                Name = "bulk_" + i, Price = i / 100m, Enabled = true, Status = ProductStatus.Ready
            }).ToArray();
            var stopwatch = Stopwatch.StartNew();
            repository.AddRange(rows);
            await work.SaveChangesAsync();
            stopwatch.Stop();
            TestContext.WriteLine($"{database}: AddRange + SaveChanges, 10000 rows: {stopwatch.Elapsed.TotalSeconds:F3}s");
            db.ChangeTracker.Clear();
            await repository.Query().ExecuteDeleteAsync();
            stopwatch.Restart();
            await writer.BulkInsertAsync(rows);
            stopwatch.Stop();
            TestContext.WriteLine($"{database}: native BulkInsert, 10000 rows: {stopwatch.Elapsed.TotalSeconds:F3}s (simple local table; not a throughput guarantee)");
            Assert.AreEqual(10000, await repository.CountAsync());
            Assert.AreEqual(0, db.ChangeTracker.Entries().Count());
            Assert.AreEqual(ProductStatus.Ready, (await repository.FirstOrDefaultAsync(p => p.Name == "bulk_42"))!.Status);
            Assert.AreEqual(10000, await repository.Query().ExecuteUpdateAsync(s => s.SetProperty(p => p.Enabled, false)));
            await repository.Query().ExecuteDeleteAsync();

            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await writer.BulkInsertAsync(new[] { new Product { Name = "rollback" } });
                Assert.AreEqual(1, await repository.CountAsync());
                await transaction.RollbackAsync();
            }
            Assert.AreEqual(0, await repository.CountAsync());

            // A second-chunk error must roll back the successful first chunk too.
            var invalid = Enumerable.Range(0, 1001).Select(i => new Product
                { Name = i == 1000 ? new string('x', 200) : "valid" }).ToArray();
            Exception? failure = null;
            try { await writer.BulkInsertAsync(invalid); }
            catch (Exception ex) { failure = ex; }
            Assert.IsNotNull(failure, "Oversized value must not be silently accepted.");
            Assert.AreEqual(0, await repository.CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
