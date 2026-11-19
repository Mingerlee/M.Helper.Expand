using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace M.Simple.Expand.Test;

[TestClass]
public class ReflectionExtensionsTests
{
    [TestMethod]
    public void NamedPropertyAccess_PreservesNullAndEnforcesPublicAccessors()
    {
        var model = new Child();
        model.SetPropertyValue(nameof(Child.Number), "42");
        Assert.AreEqual(42, model.GetPropertyValue<int?>(nameof(Child.Number)));
        model.SetPropertyValue(nameof(Child.Number), null);
        Assert.IsNull(model.GetPropertyValue<int?>(nameof(Child.Number)));
        Assert.ThrowsException<NullReferenceException>(() => model.GetPropertyValue<int>(nameof(Child.Number)));
        model.SetPropertyValue(nameof(Child.Anything), "raw string");
        Assert.AreEqual("raw string", model.Anything);
        model.SetPropertyValue(nameof(Child.Label), "inherited");
        Assert.AreEqual("inherited", model.Label);
        Assert.ThrowsException<MissingMemberException>(() => model.SetPropertyValue(nameof(Child.Secret), "no"));
        Assert.ThrowsException<MissingMemberException>(() => model.GetPropertyValue<int>("Item"));
        Assert.ThrowsException<MissingMemberException>(() => model.GetPropertyValue<int>(nameof(Child.StaticValue)));
        Assert.ThrowsException<InvalidCastException>(() => model.GetPropertyValue<int>(nameof(Child.Label)));
        Assert.ThrowsException<ArgumentException>(() => model.SetPropertyValue(nameof(Child.RequiredNumber), null));
        Assert.ThrowsException<ArgumentException>(() => model.SetPropertyValue(nameof(Child.RequiredNumber), 1L));
        Assert.ThrowsException<ArgumentException>(() => model.SetPropertyValue(nameof(Child.RequiredNumber), "invalid"));
        Assert.AreEqual(7, model.RequiredNumber);
    }

    [TestMethod]
    public void PropertyMetadata_ExcludesStaticIndexersAndPrivateAccessors()
    {
        Assert.IsTrue(typeof(Child).GetProperty(nameof(Child.Secret))!.IsPublicReadable());
        Assert.IsFalse(typeof(Child).GetProperty(nameof(Child.Secret))!.IsPublicWritable());
        Assert.IsFalse(typeof(Child).GetProperty("Item")!.IsPublicReadable());
        Assert.IsFalse(typeof(Child).GetProperty(nameof(Child.StaticValue))!.IsPublicReadable());
        Assert.IsFalse(typeof(Child).GetProperty(nameof(Child.WriteOnly))!.IsPublicReadable());
    }

    [TestMethod]
    public void AttributesAndDisplayNames_UsePrecedenceAndInheritance()
    {
        var inherited = typeof(Child).GetProperty(nameof(Child.Label))!;
        Assert.IsNotNull(inherited.GetAttribute<DescriptionAttribute>());
        Assert.AreEqual(1, inherited.GetAttributes<DescriptionAttribute>().Count);
        Assert.AreEqual("显示名称", inherited.GetMemberDisplayName());
        Assert.AreEqual("说明", typeof(Child).GetProperty(nameof(Child.DescriptionOnly))!.GetMemberDisplayName());
        Assert.AreEqual(nameof(Child.Anything), typeof(Child).GetProperty(nameof(Child.Anything))!.GetMemberDisplayName());
        Assert.AreEqual("类标记", typeof(Child).GetAttribute<DescriptionAttribute>()!.Description);
        Assert.IsNull(typeof(Child).GetAttribute<DescriptionAttribute>(inherit: false));
        Assert.IsNull(inherited.GetAttribute<ObsoleteAttribute>());
        Assert.AreEqual(0, inherited.GetAttributes<ObsoleteAttribute>().Count);
    }

    [TestMethod]
    public void PropertyDictionary_IsShallowAndSkipsUnsafeProperties()
    {
        var child = new Child { Anything = new object() };
        var dictionary = child.ToPropertyDictionary();
        Assert.IsTrue(dictionary.ContainsKey(nameof(Child.Number)));
        Assert.IsNull(dictionary[nameof(Child.Number)]);
        Assert.AreSame(child.Anything, dictionary[nameof(Child.Anything)]);
        Assert.IsFalse(dictionary.ContainsKey("Item"));
        Assert.IsFalse(dictionary.ContainsKey(nameof(Child.StaticValue)));
        Assert.IsFalse(dictionary.ContainsKey(nameof(Child.WriteOnly)));
        var filtered = child.ToPropertyDictionary(ignoreNull: true, useDisplayNames: true,
            excludedProperties: [nameof(Child.Secret)]);
        Assert.IsFalse(filtered.ContainsKey(nameof(Child.Number)));
        Assert.IsFalse(filtered.ContainsKey(nameof(Child.Secret)));
        Assert.AreEqual("base", filtered["显示名称"]);
        Assert.ThrowsException<ArgumentException>(() => new DuplicateNames().ToPropertyDictionary(useDisplayNames: true));
    }

    [TestMethod]
    public void TypeClassification_HandlesNullableEnumsDatesAndComplexTypes()
    {
        foreach (var type in new[] { typeof(int?), typeof(DayOfWeek?), typeof(DateOnly), typeof(TimeOnly), typeof(Guid), typeof(decimal) })
            Assert.IsTrue(type.IsSimpleType(), type.Name);
        foreach (var type in new[] { typeof(void), typeof(object), typeof(List<int>), typeof(Child) })
            Assert.IsFalse(type.IsSimpleType(), type.Name);
        Assert.IsTrue(typeof(int?).IsNullableType());
        Assert.IsFalse(typeof(string).IsNullableType());
        Assert.AreEqual(typeof(int), typeof(int?).GetNonNullableType());
        Assert.AreEqual(typeof(string), typeof(string).GetNonNullableType());
    }

    [TestMethod]
    public void GenericHierarchy_PreservesAllConstructedInterfaces()
    {
        Assert.AreEqual(typeof(GenericParent<int>), typeof(GenericChild).GetGenericBaseType(typeof(GenericParent<>)));
        Assert.AreEqual(typeof(GenericParent<int>), typeof(GenericParent<int>).GetGenericBaseType(typeof(GenericParent<>)));
        CollectionAssert.AreEquivalent(new[] { typeof(IContract<int>), typeof(IContract<string>) },
            typeof(GenericChild).GetGenericInterfaces(typeof(IContract<>)).ToArray());
        CollectionAssert.AreEqual(new[] { typeof(IContract<int>) },
            typeof(IContract<int>).GetGenericInterfaces(typeof(IContract<>)).ToArray());
        Assert.IsNull(typeof(Child).GetGenericBaseType(typeof(GenericParent<>)));
        Assert.ThrowsException<ArgumentException>(() => typeof(Child).GetGenericBaseType(typeof(GenericParent<int>)));
        Assert.ThrowsException<ArgumentException>(() => typeof(Child).GetGenericInterfaces(typeof(GenericParent<>)));
    }

    [TestMethod]
    public void AssemblyScanning_ExcludesAbstractOpenGenericAndInvisibleClasses()
    {
        CollectionAssert.AreEquivalent(new[] { typeof(ScanImplementation) },
            typeof(ScanImplementation).Assembly.GetImplementations<IScanContract>().ToArray());
        var assembly = new PartiallyLoadableAssembly();
        CollectionAssert.AreEqual(new[] { typeof(ScanImplementation) }, assembly.GetLoadableTypes().ToArray());
    }

    private sealed class PartiallyLoadableAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException(
            [typeof(ScanImplementation), null!], [new TypeLoadException("unavailable")]);
    }

    [System.ComponentModel.Description("类标记")]
    public class Parent
    {
        [Display(Name = "显示名称"), DisplayName("备用名称"), System.ComponentModel.Description("描述")]
        public virtual string Label { get; set; } = "base";
    }

    public sealed class Child : Parent
    {
        public int? Number { get; set; }
        public int RequiredNumber { get; set; } = 7;
        public object Anything { get; set; }
        public string Secret { get; private set; } = "secret";
        [System.ComponentModel.Description("说明")] public string DescriptionOnly { get; set; } = "description";
        public static int StaticValue => throw new InvalidOperationException();
        public int this[int index] => throw new InvalidOperationException();
        public string WriteOnly { private get; set; }
    }

    public class DuplicateNames
    {
        [DisplayName("same")] public int First { get; set; }
        [DisplayName("same")] public int Second { get; set; }
    }

    public interface IContract<T> { }
    public class GenericParent<T> : IContract<T> { }
    public sealed class GenericChild : GenericParent<int>, IContract<string> { }
    public interface IScanContract { }
    public abstract class ScanAbstract : IScanContract { }
    public class ScanGeneric<T> : IScanContract { }
    private sealed class ScanHidden : IScanContract { }
    public sealed class ScanImplementation : IScanContract { }
}
