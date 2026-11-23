using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public class ConstructorBindingTests
{
    [TestMethod]
    public void SharedMetadataDoesNotShareProviderInstancesOrKeys()
    {
        var first = new ServiceCollection();
        first.AddSingleton(new Value("first"));
        first.AddKeyedScoped<Consumer>("a");
        var second = new ServiceCollection();
        second.AddSingleton(new Value("second"));
        second.AddKeyedScoped<Consumer>("b");
        using var a = first.BuildSimpleServiceProvider();
        using var b = second.BuildSimpleServiceProvider();
        using var scopeA = a.CreateScope();
        using var scopeB = b.CreateScope();
        var consumerA = scopeA.ServiceProvider.GetRequiredKeyedService<Consumer>("a");
        var consumerB = scopeB.ServiceProvider.GetRequiredKeyedService<Consumer>("b");
        Assert.AreEqual("first", consumerA.Value.Text);
        Assert.AreEqual("second", consumerB.Value.Text);
        Assert.AreEqual("a", consumerA.Key);
        Assert.AreEqual("b", consumerB.Key);
        Assert.AreNotSame(consumerA.Value, consumerB.Value);
    }

    [TestMethod]
    public void ConstructorsWithZeroThroughManyArgumentsKeepBindings()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Value("value"));
        services.AddTransient<Zero>();
        services.AddTransient<One>();
        services.AddTransient<Two>();
        services.AddTransient<Three>();
        services.AddTransient<Four>();
        services.AddTransient<Many>();
        using var root = services.BuildSimpleServiceProvider();
        Assert.IsNotNull(root.GetRequiredService<Zero>());
        Assert.AreEqual("value", root.GetRequiredService<One>().Value.Text);
        Assert.AreEqual(2, root.GetRequiredService<Two>().Number);
        Assert.AreEqual("three", root.GetRequiredService<Three>().Text);
        Assert.AreEqual(default(DateTime), root.GetRequiredService<Four>().Date);
        var many = root.GetRequiredService<Many>();
        Assert.AreEqual("value", many.Value.Text);
        Assert.AreEqual(5, many.Number);
        Assert.AreEqual("six", many.Text);
    }

    [TestMethod]
    public void UserConstructorExceptionsAreNotWrapped()
    {
        var failure = new Failure(new InvalidOperationException("constructor failed"));
        var services = new ServiceCollection();
        services.AddSingleton(failure);
        services.AddTransient<Throws>();
        using var root = services.BuildSimpleServiceProvider();
        var thrown = Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<Throws>());
        Assert.AreSame(failure.Exception, thrown);
    }

    [TestMethod]
    public void DefaultStructDoesNotRunUserParameterlessConstructor()
    {
        var services = new ServiceCollection();
        services.AddTransient<StructDefault>();
        using var root = services.BuildSimpleServiceProvider();
        Assert.AreEqual(0, root.GetRequiredService<StructDefault>().Value.Number);
    }

    [TestMethod]
    public void SharedMetadataDoesNotRetainDisposedProviderOrDependencies()
    {
        var references = CreateAndRelease();
        for (var i = 0; i < 5 && (references.Provider.IsAlive || references.Value.IsAlive); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.IsFalse(references.Provider.IsAlive);
        Assert.IsFalse(references.Value.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Provider, WeakReference Value) CreateAndRelease()
    {
        var services = new ServiceCollection();
        var value = new Value("collectible");
        services.AddSingleton(value);
        services.AddTransient<One>();
        using var root = services.BuildSimpleServiceProvider();
        root.GetRequiredService<One>();
        return (new WeakReference(root), new WeakReference(value));
    }

    public record Value(string Text);
    public record Consumer(Value Value, [ServiceKey] string Key);
    public class Zero;
    public record One(Value Value);
    public record Two(Value Value, int Number = 2);
    public record Three(Value Value, int Number = 3, string Text = "three");
    public record Four(Value Value, int Number = 4, string Text = "four", DateTime Date = default);
    public record Many(One One, Two Two, Three Three, Four Four, Value Value, int Number = 5, string Text = "six");
    public record Failure(Exception Exception);
    public class Throws
    {
        public Throws(Failure failure, int a = 1, int b = 2, int c = 3, int d = 4) => throw failure.Exception;
    }
    public struct CustomDefault
    {
        public int Number;
        public CustomDefault() { Number = 99; }
    }
    public record StructDefault(CustomDefault Value = default);
}
