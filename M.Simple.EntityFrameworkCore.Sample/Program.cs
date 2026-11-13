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
await using var transaction = await db.Database.BeginTransactionAsync();
var products = scope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();
var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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
await transaction.RollbackAsync();
Console.WriteLine("Sample row changes rolled back. The dedicated database/schema remains for reuse.");

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
