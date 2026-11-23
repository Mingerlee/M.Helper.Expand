using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public class ScanningTests
{
    [TestMethod]
    public void MarkerScanRecognizesInheritedInterfacesAndSharesInstance()
    {
        var services = new ServiceCollection();
        services.AddAssemblyServices<IMarker>(typeof(ScanningTests).Assembly);
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        Assert.AreSame<object>(scope.ServiceProvider.GetRequiredService<IUser>(), scope.ServiceProvider.GetRequiredService<IAdmin>());
        Assert.AreSame(scope.ServiceProvider.GetRequiredService<IUser>(), scope.ServiceProvider.GetRequiredService<IUser>());
        Assert.IsNull(scope.ServiceProvider.GetService<IMarker>());
        Assert.IsNull(scope.ServiceProvider.GetService<IDisposable>());
        Assert.IsNull(scope.ServiceProvider.GetService<Other>());
        using var other = root.CreateScope();
        Assert.AreNotSame(scope.ServiceProvider.GetRequiredService<IUser>(), other.ServiceProvider.GetRequiredService<IUser>());
    }

    [TestMethod]
    public void FluentScanCanFilterAndExposeSelfAndInterfaces()
    {
        var services = new ServiceCollection();
        services.Scan(typeof(ScanningTests).Assembly).AssignableTo<IUser>()
            .Where(t => t == typeof(User)).AsImplementedInterfaces().AsSelf().Singleton();
        using var root = services.BuildSimpleServiceProvider();
        Assert.AreSame(root.GetRequiredService<IUser>(), root.GetRequiredService<User>());
        Assert.AreSame<object>(root.GetRequiredService<IUser>(), root.GetRequiredService<IAdmin>());
    }

    [TestMethod]
    public void RepeatedScansAreIdempotent()
    {
        var services = new ServiceCollection();
        services.AddAssemblyServices<IMarker>(typeof(ScanningTests).Assembly);
        var count = services.Count;
        services.AddAssemblyServices<IMarker>(typeof(ScanningTests).Assembly);
        Assert.AreEqual(count, services.Count);
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        Assert.AreEqual(1, scope.ServiceProvider.GetServices<IUser>().Count());
        Assert.AreSame<object>(scope.ServiceProvider.GetRequiredService<IUser>(), scope.ServiceProvider.GetRequiredService<IAdmin>());
    }

    [TestMethod]
    public void LaterScanCanExposeSelfWithoutBreakingExistingInterfaceIdentity()
    {
        var services = new ServiceCollection();
        services.AddAssemblyServices<IMarker>(typeof(ScanningTests).Assembly);
        services.Scan(typeof(ScanningTests).Assembly).Where(t => t == typeof(User)).AsSelf().Scoped();
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        Assert.AreSame<object>(scope.ServiceProvider.GetRequiredService<IUser>(), scope.ServiceProvider.GetRequiredService<User>());
    }

    [TestMethod]
    public void ScannedAliasesDisposeOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddAssemblyServices<IMarker>(typeof(ScanningTests).Assembly);
        using var root = services.BuildSimpleServiceProvider();
        User value;
        using (var scope = root.CreateScope())
        {
            value = (User)scope.ServiceProvider.GetRequiredService<IUser>();
            scope.ServiceProvider.GetRequiredService<IAdmin>();
        }
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public void GenericScannedAliasesSharePerClosedImplementation()
    {
        var services = new ServiceCollection();
        services.Scan(typeof(ScanningTests).Assembly).AssignableTo(typeof(IRepository<>))
            .AsImplementedInterfaces().Scoped();
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        Assert.AreSame<object>(scope.ServiceProvider.GetRequiredService<IRepository<int>>(), scope.ServiceProvider.GetRequiredService<IReadRepository<int>>());
        Assert.AreNotSame<object>(scope.ServiceProvider.GetRequiredService<IRepository<int>>(), scope.ServiceProvider.GetRequiredService<IRepository<string>>());
    }

    [TestMethod]
    public void TransientScansCreateEachTime()
    {
        var services = new ServiceCollection();
        services.Scan(typeof(ScanningTests).Assembly).Where(t => t == typeof(User)).AsImplementedInterfaces().Transient();
        using var root = services.BuildSimpleServiceProvider();
        Assert.AreNotSame(root.GetRequiredService<IUser>(), root.GetRequiredService<IUser>());
    }

    [TestMethod]
    public void EmptyScanMustBeExplicitlyRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => new ServiceCollection().Scan());
    }

    public interface IMarker;
    public interface IUser : IMarker;
    public interface IAdmin : IMarker;
    public class User : IUser, IAdmin, IDisposable
    {
        public int Disposals;
        public void Dispose() => Disposals++;
    }
    public class Other;
    public interface IRepository<T>;
    public interface IReadRepository<T>;
    public class Repository<T> : IRepository<T>, IReadRepository<T>;
}
