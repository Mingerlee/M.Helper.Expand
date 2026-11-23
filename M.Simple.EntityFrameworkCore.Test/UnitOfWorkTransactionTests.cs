using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.EntityFrameworkCore.Test;

[TestClass]
public class UnitOfWorkTransactionTests
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TestContext Db => _scope.ServiceProvider.GetRequiredService<TestContext>();
    private IUnitOfWork Work => _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    private IRepository<Product, long> Products => _scope.ServiceProvider.GetRequiredService<IRepository<Product, long>>();

    [TestInitialize]
    public async Task Initialize()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<TestContext>(o => o.UseSqlite(_connection));
        services.AddSimpleEfCore<TestContext>();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
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
    public async Task MissingBegin_IsRejectedBeforeAnyWrites()
    {
        Products.Add(new Product { Name = "pending" });
        var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.SaveChangesAndCommitAsync());
        StringAssert.Contains(error.Message, "请先调用 BeginTransactionAsync");
        Assert.AreEqual(0, await Products.CountAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.CommitTransactionAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.RollbackTransactionAsync());
        Assert.AreEqual(1, await Work.SaveChangesAsync()); // Existing behavior remains available without Begin.
    }

    [TestMethod]
    public async Task SaveAndCommit_PersistsRowsAndAllowsAnotherTransaction()
    {
        await Work.BeginTransactionAsync();
        Products.Add(new Product { Name = "commit" });
        Assert.AreEqual(1, await Work.SaveChangesAndCommitAsync());
        Assert.IsNull(Db.Database.CurrentTransaction);
        Assert.AreEqual(1, await Products.CountAsync());
        await Work.BeginTransactionAsync();
        await Work.RollbackTransactionAsync();
    }

    [TestMethod]
    public async Task MultipleSaves_AreRolledBackTogether()
    {
        await Work.BeginTransactionAsync();
        foreach (var name in new[] { "before", "business", "after" })
        {
            Products.Add(new Product { Name = name });
            await Work.SaveChangesAsync();
            Assert.IsNotNull(Db.Database.CurrentTransaction);
        }
        Assert.AreEqual(3, await Products.CountAsync());
        await Work.RollbackTransactionAsync();
        Assert.AreEqual(0, await Products.CountAsync());
        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
    }

    [TestMethod]
    public async Task Rollback_DiscardsAllPendingChangesAndAllowsFreshWork()
    {
        var updated = new Product { Name = "original" };
        var deleted = new Product { Name = "keep" };
        Products.Add(updated);
        Products.Add(deleted);
        await Work.SaveChangesAsync();
        await Work.BeginTransactionAsync();
        updated.Name = "changed";
        Db.Remove(deleted);
        Products.Add(new Product { Name = "unsaved" });

        await Work.RollbackTransactionAsync();

        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
        Assert.AreEqual("changed", updated.Name); // Detaching does not restore object values.
        Assert.AreEqual(0, await Work.SaveChangesAsync());
        Assert.AreEqual("original", (await Products.GetByIdAsync(updated.Id))!.Name);
        Assert.AreEqual(2, await Products.CountAsync());
        await Work.BeginTransactionAsync();
        Products.Add(new Product { Name = "fresh" });
        Assert.AreEqual(1, await Work.SaveChangesAndCommitAsync());
        Assert.AreEqual(3, await Products.CountAsync());
    }

    [TestMethod]
    public async Task FailedRollback_PreservesTrackingUntilSuccessfulRollback()
    {
        await Work.BeginTransactionAsync();
        var pending = new Product { Name = "pending" };
        Products.Add(pending);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { await Work.RollbackTransactionAsync(cancellation.Token); Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException) { }
        Assert.AreEqual(EntityState.Added, Db.Entry(pending).State);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.SaveChangesAsync());
        await Work.RollbackTransactionAsync(CancellationToken.None);
        Assert.AreEqual(0, Db.ChangeTracker.Entries().Count());
        Assert.AreEqual(0, await Work.SaveChangesAsync());
    }

    [TestMethod]
    public async Task CommitDoesNotSavePendingChanges()
    {
        await Work.BeginTransactionAsync();
        Products.Add(new Product { Name = "unsaved" });
        await Work.CommitTransactionAsync();
        Assert.AreEqual(0, await Products.CountAsync());
        Assert.AreEqual(EntityState.Added, Db.ChangeTracker.Entries().Single().State);
    }

    [TestMethod]
    public async Task ExistingAndExternalTransactions_CannotBeNestedOrAdopted()
    {
        await Work.BeginTransactionAsync();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.BeginTransactionAsync());
        await Work.RollbackTransactionAsync();
        await using var external = await Db.Database.BeginTransactionAsync();
        Products.Add(new Product { Name = "external" });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.BeginTransactionAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.SaveChangesAndCommitAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.CommitTransactionAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.RollbackTransactionAsync());
        Assert.AreSame(external, Db.Database.CurrentTransaction);
        await Work.SaveChangesAsync(); // External owner can still use ordinary SaveChanges.
        await external.RollbackAsync();
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task FailedSave_PreventsCommitAndStillAllowsRollbackOfEarlierSaves()
    {
        await Work.BeginTransactionAsync();
        Products.Add(new Product { Name = "first" });
        await Work.SaveChangesAsync();
        Products.Add(new Product { Name = null! }); // NOT NULL database constraint.
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => Work.SaveChangesAndCommitAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.CommitTransactionAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.SaveChangesAsync());
        await Work.RollbackTransactionAsync();
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task CancelledCommit_IsNotRetriedAndCanBeRolledBackWithIndependentToken()
    {
        await Work.BeginTransactionAsync();
        Products.Add(new Product { Name = "cancelled" });
        await Work.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { await Work.CommitTransactionAsync(cancellation.Token); Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException) { }
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.CommitTransactionAsync());
        await Work.RollbackTransactionAsync(CancellationToken.None);
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task DisposingUnitOfWork_RollsBackUncommittedChanges()
    {
        var work = Work;
        await work.BeginTransactionAsync();
        Products.Add(new Product { Name = "uncommitted" });
        await work.SaveChangesAsync();
        ((IDisposable)work).Dispose();
        Assert.IsNull(Db.Database.CurrentTransaction);
        Assert.AreEqual(0, await Products.CountAsync());
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => work.BeginTransactionAsync());
    }

    [TestMethod]
    public async Task AsyncDisposal_RollsBackWithoutOwningOrDisposingTheDbContext()
    {
        var work = Work;
        await work.BeginTransactionAsync();
        Products.Add(new Product { Name = "async-dispose" });
        await work.SaveChangesAsync();
        await ((IAsyncDisposable)work).DisposeAsync();
        Assert.IsNull(Db.Database.CurrentTransaction);
        Assert.AreEqual(0, await Products.CountAsync());
    }

    [TestMethod]
    public async Task ExternalTransactionEnd_IsDetectedBeforeSaving()
    {
        await Work.BeginTransactionAsync();
        await Db.Database.CurrentTransaction!.DisposeAsync();
        Products.Add(new Product { Name = "must-not-save" });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Work.SaveChangesAndCommitAsync());
        Assert.AreEqual(0, await Products.CountAsync());
    }
}
