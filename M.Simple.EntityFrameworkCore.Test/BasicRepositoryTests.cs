using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.EntityFrameworkCore.Test;

[TestClass]
public class BasicRepositoryTests
{
    [TestMethod]
    public async Task ShortRepositoriesShareContextAndSupportTypedKeyOperations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<ShortKeyContext>(options => options.UseSqlite(connection));
        services.AddSimpleEfCore<ShortKeyContext>();
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShortKeyContext>();
        await db.Database.EnsureCreatedAsync();

        var ints = scope.ServiceProvider.GetRequiredService<IBasicIntRepository<IntRecord>>();
        var longs = scope.ServiceProvider.GetRequiredService<IBasicLongRepository<LongRecord>>();
        Assert.AreSame(ints, scope.ServiceProvider.GetRequiredService<IBasicIntRepository<IntRecord>>());
        Assert.AreSame(longs, scope.ServiceProvider.GetRequiredService<IBasicLongRepository<LongRecord>>());
        IBasicRepository<IntRecord, int> typedInts = ints;
        IBasicRepository<LongRecord, long> typedLongs = longs;
        var generalInts = scope.ServiceProvider.GetRequiredService<IBasicRepository<IntRecord, int>>();
        var generalLongs = scope.ServiceProvider.GetRequiredService<IBasicRepository<LongRecord, long>>();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var intRecord = new IntRecord { Name = "int" };
        var longRecord = new LongRecord { Name = "long" };
        ints.Add(intRecord);
        longs.Add(longRecord);
        Assert.AreEqual(0, await typedInts.CountAsync());
        Assert.AreEqual(0, await typedLongs.CountAsync());
        await work.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.AreEqual("int", (await ints.GetByIdAsync(intRecord.Id))!.Name);
        Assert.AreEqual("long", (await longs.GetByIdAsync(longRecord.Id))!.Name);
        Assert.AreEqual(0, db.ChangeTracker.Entries().Count());

        var trackedInt = await generalInts.GetByIdAsync(intRecord.Id, tracking: true);
        var trackedLong = await generalLongs.GetByIdAsync(longRecord.Id, tracking: true);
        ints.Update(new IntRecord { Id = intRecord.Id, Name = "updated int" });
        longs.Update(new LongRecord { Id = longRecord.Id, Name = "updated long" });
        Assert.AreEqual("updated int", trackedInt!.Name);
        Assert.AreEqual("updated long", trackedLong!.Name);
        Assert.AreEqual(2, db.ChangeTracker.Entries().Count());
        await work.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.AreEqual("updated int", (await generalInts.GetByIdAsync(intRecord.Id))!.Name);
        Assert.AreEqual("updated long", (await generalLongs.GetByIdAsync(longRecord.Id))!.Name);

        Assert.IsTrue(await ints.RemoveByIdAsync(intRecord.Id));
        Assert.IsTrue(await longs.RemoveByIdAsync(longRecord.Id));
        Assert.AreEqual(1, await generalInts.CountAsync());
        Assert.AreEqual(1, await generalLongs.CountAsync());
        await work.SaveChangesAsync();
        Assert.IsNull(await generalInts.GetByIdAsync(intRecord.Id));
        Assert.IsNull(await generalLongs.GetByIdAsync(longRecord.Id));
        Assert.IsFalse(await ints.RemoveByIdAsync(intRecord.Id));
        Assert.IsFalse(await longs.RemoveByIdAsync(longRecord.Id));
    }

    public sealed class IntRecord : Entity<int>
    {
        public string Name { get; set; } = "";
    }

    public sealed class LongRecord : Entity<long>
    {
        public string Name { get; set; } = "";
    }

    public sealed class ShortKeyContext(DbContextOptions<ShortKeyContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntRecord>();
            modelBuilder.Entity<LongRecord>();
        }
    }
}
