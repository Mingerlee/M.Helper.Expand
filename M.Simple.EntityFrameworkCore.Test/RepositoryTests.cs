using System.Data;
using System.Data.Common;
using M.Simple.EntityFrameworkCore.Infrastructure;
using M.Simple.EntityFrameworkCore.MySql;
using M.Simple.EntityFrameworkCore.SqlServer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.EntityFrameworkCore.Test;

[TestClass]
public class RepositoryTests
{
    private SqliteConnection connection = null!;
    private ServiceProvider provider = null!;
    private IServiceScope scope = null!;
    private TestContext Db => scope.ServiceProvider.GetRequiredService<TestContext>();
    private IRepository<Product, long> Products => scope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();
    private IUnitOfWork Work => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    [TestInitialize]
    public async Task Initialize()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<TestContext>(o => o.UseSqlite(connection));
        services.AddSimpleEfCore<TestContext>();
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        scope = provider.CreateScope();
        await Db.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        scope.Dispose();
        await provider.DisposeAsync();
        await connection.DisposeAsync();
    }

    [TestMethod]
    public async Task CrudRequiresSaveAndDefaultsToNoTracking()
    {
        var product = new Product { Name = "original", Price = 12 };
        Products.Add(product);
        Assert.AreEqual(0, await Products.CountAsync());
        await Work.SaveChangesAsync();
        Assert.IsTrue(product.Id > 0);
        Db.ChangeTracker.Clear();
        Assert.IsNotNull(await Products.GetByIdAsync(product.Id));
        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
        var tracked = await Products.GetByIdAsync(product.Id, true);
        tracked!.Name = "updated";
        await Work.SaveChangesAsync();
        Assert.IsTrue(await Products.AnyAsync(p => p.Name == "updated"));
        Assert.IsTrue(await Products.RemoveByIdAsync(product.Id));
        Assert.AreEqual(1, await Products.CountAsync());
        await Work.SaveChangesAsync();
        Assert.AreEqual(0, await Products.CountAsync());
        Assert.IsFalse(await Products.RemoveByIdAsync(product.Id));
    }

    [TestMethod]
    public async Task RepositoriesShareOneUnitOfWork()
    {
        Products.AddRange([new() { Name = "a" }, new() { Name = "b" }]);
        var labels = scope.ServiceProvider.GetRequiredService<IRepository<Label, Guid>>();
        var id = Guid.NewGuid();
        labels.Add(new Label { Id = id, Text = "label" });
        await Work.SaveChangesAsync();
        Assert.AreEqual(2, await Products.CountAsync());
        Assert.AreEqual("label", (await labels.GetByIdAsync(id))!.Text);
        Products.RemoveRange(await Products.ListAsync());
        await Work.SaveChangesAsync();
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task PartialUpdatePreservesOtherColumns()
    {
        var entity = new Product { Name = "keep", Price = 10 };
        Products.Add(entity);
        await Work.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        Products.UpdateProperties(new Product { Id = entity.Id, Price = 25 }, p => p.Price);
        await Work.SaveChangesAsync();
        var result = await Products.GetByIdAsync(entity.Id);
        Assert.AreEqual("keep", result!.Name);
        Assert.AreEqual(25m, result.Price);
    }

    [TestMethod]
    public void InvalidPartialUpdateDoesNotAttachEntity()
    {
        var entity = new Product { Id = 1 };
        Assert.ThrowsException<ArgumentException>(() => Products.UpdateProperties(entity, p => p.Price, p => p.Id));
        Assert.ThrowsException<ArgumentException>(() => Products.UpdateProperties(entity, p => p.Name.Length));
        Assert.AreEqual(EntityState.Detached, Db.Entry(entity).State);
    }

    [TestMethod]
    public async Task PartialUpdateCanReuseAlreadyTrackedEntity()
    {
        var entity = new Product { Name = "keep", Price = 10 };
        Products.Add(entity);
        await Work.SaveChangesAsync();
        Products.UpdateProperties(new Product { Id = entity.Id, Price = 30 }, p => p.Price);
        await Work.SaveChangesAsync();
        Assert.AreEqual(30m, entity.Price);
        Assert.AreEqual("keep", entity.Name);
        Assert.AreEqual(1, Db.ChangeTracker.Entries<Product>().Count());
    }

    [TestMethod]
    public async Task RemovingAnUnsavedEntityDoesNotRemoveAnotherTemporaryKey()
    {
        var first = new Product { Name = "keep" };
        var second = new Product { Name = "remove" };
        Products.AddRange([first, second]);
        Products.Remove(second);
        await Work.SaveChangesAsync();
        Assert.AreEqual("keep", (await Products.ListAsync()).Single().Name);
    }

    [TestMethod]
    public async Task PageSupportsFilteringProjectionAndEmptyPages()
    {
        Products.AddRange(Enumerable.Range(1, 7).Select(i => new Product { Name = "p" + i, Enabled = i > 2 }));
        await Work.SaveChangesAsync();
        var query = Products.Query().WhereIf(true, p => p.Enabled).WhereIf(false, p => false)
            .OrderBy(p => p.Id).Select(p => p.Name);
        var page = await query.ToPageAsync(2, 2);
        Assert.AreEqual(5, page.TotalCount);
        Assert.AreEqual(3L, page.TotalPages);
        CollectionAssert.AreEqual(new[] { "p5", "p6" }, page.Items.ToArray());
        Assert.AreEqual(0, (await query.ToPageAsync(10, 2)).Items.Count);
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => query.ToPageAsync(0));
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => query.ToPageAsync(1, 201));
        Assert.AreEqual(7, (await Products.Query().OrderBy(p => p.Id).ToPageAsync(1, 300,
            options: new SimpleEfCoreOptions { MaxPageSize = 300 })).Items.Count);
        await Assert.ThrowsExceptionAsync<OverflowException>(() => query.ToPageAsync(int.MaxValue, 200));
    }

    [TestMethod]
    public async Task QueryAndBulkHonorCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Products.CountAsync(cancellationToken: cts.Token));
        var writer = new TestBulkWriter(scope.ServiceProvider.GetRequiredService<ISimpleDbContext>());
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => writer.BulkInsertAsync(new[] { new Product() }, cts.Token));
    }

    [TestMethod]
    public async Task BulkPipelineBatchesAndRollsBackOnFailure()
    {
        var writer = new TestBulkWriter(scope.ServiceProvider.GetRequiredService<ISimpleDbContext>()) { FailOnBatch = 2 };
        var items = Enumerable.Range(0, 1001).Select(i => new Product { Name = "bulk" + i });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => writer.BulkInsertAsync(items));
        Assert.AreEqual(0, await Products.CountAsync());
        writer = new TestBulkWriter(scope.ServiceProvider.GetRequiredService<ISimpleDbContext>());
        await writer.BulkInsertAsync(items);
        Assert.AreEqual(1001, await Products.CountAsync());
        CollectionAssert.AreEqual(new[] { 1000, 1 }, writer.BatchSizes.ToArray());
        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
    }

    [TestMethod]
    public async Task BulkUsesCallerTransactionAndValueConverters()
    {
        var writer = new TestBulkWriter(scope.ServiceProvider.GetRequiredService<ISimpleDbContext>());
        await using (var transaction = await Db.Database.BeginTransactionAsync())
        {
            await writer.BulkInsertAsync(new[] { new Product { Name = "enum", Status = ProductStatus.Ready } });
            Assert.AreEqual(ProductStatus.Ready, (await Products.FirstOrDefaultAsync(p => p.Name == "enum"))!.Status);
            await transaction.RollbackAsync();
        }
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public void DuplicateContextRegistrationIsRejected()
    {
        var services = new ServiceCollection();
        services.AddSimpleEfCore<TestContext>();
        Assert.ThrowsException<InvalidOperationException>(() => services.AddSimpleEfCore<TestContext>());
    }

    [DataTestMethod]
    [DataRow("mysql")]
    [DataRow("sqlserver")]
    public void ProviderRegistrationAndSqlTranslation(string database)
    {
        var services = new ServiceCollection();
        if (database == "mysql")
            services.AddSimpleEfCoreMySql<TestContext>("Server=localhost;Database=unused;User ID=unused;Password=unused",
                new MySqlServerVersion(new Version(8, 0, 36)));
        else
            services.AddSimpleEfCoreSqlServer<TestContext>("Server=localhost;Database=unused;Integrated Security=true;TrustServerCertificate=true");
        using var container = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var serviceScope = container.CreateScope();
        var repository = serviceScope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();
        var sql = repository.Query().Where(p => p.Id.Equals(1L)).OrderBy(p => p.Id).Skip(10).Take(20).ToQueryString();
        StringAssert.Contains(sql, database == "mysql" ? "LIMIT" : "OFFSET");
        Assert.IsNotNull(serviceScope.ServiceProvider.GetRequiredService<IBulkWriter>());
        Assert.IsNotNull(serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }
}

public enum ProductStatus { Draft, Ready }
public sealed class Product : Entity<long>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool Enabled { get; set; }
    public ProductStatus Status { get; set; }
}
public sealed class Label : Entity<Guid> { public string Text { get; set; } = ""; }
public sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("ef_test_products");
        modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(100);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Label>().ToTable("ef_test_labels");
        modelBuilder.Entity<Label>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<Label>().Property(p => p.Text).HasMaxLength(100);
    }
}

// Exercises the shared batching/conversion/transaction pipeline against real relational storage.
internal sealed class TestBulkWriter(ISimpleDbContext accessor) : BulkWriterBase(accessor)
{
    public int FailOnBatch { get; init; }
    public List<int> BatchSizes { get; } = [];
    protected override async Task WriteBatchAsync(DataTable data, string table, string? schema,
        DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        BatchSizes.Add(data.Rows.Count);
        if (BatchSizes.Count == FailOnBatch) throw new InvalidOperationException("Injected batch failure.");
        foreach (DataRow row in data.Rows)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var columns = data.Columns.Cast<DataColumn>().ToArray();
            command.CommandText = $"INSERT INTO \"{table}\" ({string.Join(",", columns.Select(c => $"\"{c.ColumnName}\""))}) VALUES ({string.Join(",", columns.Select(c => "@p" + c.Ordinal))})";
            foreach (var column in columns)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p" + column.Ordinal;
                parameter.Value = row[column];
                command.Parameters.Add(parameter);
            }
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
