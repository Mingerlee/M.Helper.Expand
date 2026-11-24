using M.Simple.EntityFrameworkCore;
using M.Simple.EntityFrameworkCore.MySql;
using M.Simple.EntityFrameworkCore.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

var database = args.FirstOrDefault() ?? "sqlserver";
var connection = Environment.GetEnvironmentVariable("MSIMPLE_EF_SAMPLE_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.WriteLine("Set MSIMPLE_EF_SAMPLE_CONNECTION to a dedicated database named msimple_ef_sample_<name>.");
    Console.WriteLine("Run with sqlserver or mysql. The schema is created if needed; sample row changes are rolled back.");
    return;
}
var services = new ServiceCollection();
string databaseName;
if (database == "sqlserver")
{
    databaseName = new SqlConnectionStringBuilder(connection).InitialCatalog;
    services.AddSimpleEfCoreSqlServer<SampleContext>(connection);
}
else if (database == "mysql")
{
    databaseName = new MySqlConnectionStringBuilder(connection).Database;
    services.AddSimpleEfCoreMySql<SampleContext>(connection, ServerVersion.AutoDetect(connection));
}
else throw new ArgumentException("Choose sqlserver or mysql.");
if (!databaseName.StartsWith("msimple_ef_sample_", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Use a dedicated database named msimple_ef_sample_<name>.");

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<SampleContext>();
await db.Database.EnsureCreatedAsync();
var products = scope.ServiceProvider.GetRequiredService<IBasicLongRepository<Product>>();
var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
await work.BeginTransactionAsync();

var product = new Product { Name = "Sample", Price = 10 };
products.Add(product);
await work.SaveChangesAsync();
var found = await products.GetByIdAsync(product.Id);
Console.WriteLine($"Created: {found!.Id}, {found.Name}");

products.UpdateProperties(new Product { Id = product.Id, Price = 20 }, p => p.Price);
await work.SaveChangesAsync();
var page = await products.Query().OrderBy(p => p.Id)
    .Select(p => new { p.Id, p.Name, p.Price }).ToPageAsync();
Console.WriteLine($"Page: {page.Items.Count} / {page.TotalCount}");

await products.RemoveByIdAsync(product.Id);
await work.SaveChangesAsync();

var bulk = scope.ServiceProvider.GetRequiredService<IBulkWriter>();
await bulk.BulkInsertAsync(Enumerable.Range(1, 1000)
    .Select(i => new Product { Name = "Bulk " + i, Price = i }));
Console.WriteLine($"Rows after bulk insert: {await products.CountAsync()}");
await work.RollbackTransactionAsync();
Console.WriteLine("Sample row changes rolled back. The dedicated database/schema remains for reuse.");

// 保存并提交的独立演示，再只清理这次示例写入的记录。
await work.BeginTransactionAsync();
var committed = new Product { Name = "SaveChangesAndCommitAsync", Price = 30 };
products.Add(committed);
int affected = await work.SaveChangesAndCommitAsync();
Console.WriteLine($"Saved and committed {affected} entry; generated ID: {committed.Id}.");
db.ChangeTracker.Clear();
if (!await products.RemoveByIdAsync(committed.Id))
    throw new InvalidOperationException("Expected committed sample row was not found.");
await work.SaveChangesAsync();
Console.WriteLine("Committed sample row removed; pre-existing data was not deleted.");

// 新增 API 演示：全部操作使用同一事务，结束时回滚，不留下演示记录。
await work.BeginTransactionAsync();
try
{
    var rows = new[]
    {
        new Product { Name = "ReturnKey A", Price = 10 },
        new Product { Name = "ReturnKey B", Price = 20 }
    };
    await bulk.BulkInsertAndReturnKeysAsync(rows);
    rows[0].Name = "Update";
    products.Update(rows[0]);
    rows[1].Name = "UpdateRange";
    products.UpdateRange(rows.Skip(1));
    await work.SaveChangesAsync();
    int updated = await products.ExecuteUpdateAsync(p => p.Id == rows[0].Id,
        setters => setters.SetProperty(p => p.Price, 40m));
    int bulkUpdated = await bulk.BulkUpdateAsync(rows,
        new BulkUpdateOptions<Product> { Properties = [p => p.Price] });
    int deleted = await products.ExecuteDeleteAsync(p => p.Id == rows[0].Id);
    int bulkDeleted = await bulk.BulkDeleteAsync(rows.Skip(1));
    Console.WriteLine($"New APIs: update={updated}, bulk update={bulkUpdated}, delete={deleted}, bulk delete={bulkDeleted}.");
    await work.RollbackTransactionAsync(); // 自动清空跟踪；实体对象的主键不被还原。
}
catch (Exception original)
{
    try { await work.RollbackTransactionAsync(CancellationToken.None); }
    catch (Exception rollback) { original.Data["RollbackException"] = rollback; }
    throw;
}
public sealed class Product : Entity<long>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("sample_products");
        modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(100);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
    }
}
