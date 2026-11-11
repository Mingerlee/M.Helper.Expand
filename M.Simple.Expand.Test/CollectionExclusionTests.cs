using System.Collections;
using M.Simple.Expand;

namespace M.Simple.Expand.Test;

[TestClass]
public class CollectionExclusionTests
{
    [TestMethod]
    public void Exclusion_IntegersUseValueEquality()
    {
        var source = new[] { 1, 2, 3, 2, 4 };
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, source.ExceptItem(2).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3 }, source.ExceptItems(new[] { 2, 4 }).ToArray());
    }

    [TestMethod]
    public void Exclusion_StringsUseDefaultCaseSensitiveEquality()
    {
        var source = new[] { "a", "b", "A", "b" };
        CollectionAssert.AreEqual(new[] { "a", "A" }, source.ExceptItem("b").ToArray());
        CollectionAssert.AreEqual(new[] { "b", "A", "b" }, source.ExceptItems(new[] { "a" }).ToArray());
    }

    [TestMethod]
    public void Exclusion_CustomStringComparerIsHonored()
    {
        var source = new[] { "a", "A", "b" };
        CollectionAssert.AreEqual(new[] { "b" }, source.ExceptItem("A", StringComparer.OrdinalIgnoreCase).ToArray());
        CollectionAssert.AreEqual(new[] { "b" }, source.ExceptItems(new[] { "A" }, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [TestMethod]
    public void Exclusion_NullableValuesCanExcludeNull()
    {
        var source = new int?[] { null, 1, 2, null };
        CollectionAssert.AreEqual(new int?[] { 1, 2 }, source.ExceptItem(null).ToArray());
        CollectionAssert.AreEqual(new int?[] { 2 }, source.ExceptItems(new int?[] { null, 1 }).ToArray());
    }

    [TestMethod]
    public void Exclusion_StringNullElementsAreHandled()
    {
        var source = new[] { null, "a", "b", null };
        CollectionAssert.AreEqual(new[] { "a", "b" }, source.ExceptItem(null).ToArray());
        CollectionAssert.AreEqual(new[] { "b" }, source.ExceptItems(new[] { null, "a" }).ToArray());
    }

    [TestMethod]
    public void Exclusion_NullOrEmptySourceReturnsEmpty()
    {
        IEnumerable<int> source = null;
        Assert.AreEqual(0, source.ExceptItem(1).Count());
        Assert.AreEqual(0, source.ExceptItems(new[] { 1 }).Count());
        Assert.AreEqual(0, Array.Empty<int>().ExceptItem(1).Count());
        Assert.AreEqual(0, Array.Empty<int>().ExceptItems(new[] { 1 }).Count());
    }

    [TestMethod]
    public void ExceptItems_NullOrEmptyExclusionsPreserveOrderAndDuplicates()
    {
        var source = new[] { 3, 1, 3, 2 };
        CollectionAssert.AreEqual(source, source.ExceptItems(null).ToArray());
        CollectionAssert.AreEqual(source, source.ExceptItems(Array.Empty<int>()).ToArray());
    }

    [TestMethod]
    public void Exclusion_NoMatchPreservesOrderAndDuplicates()
    {
        var source = new[] { 3, 1, 3, 2 };
        CollectionAssert.AreEqual(source, source.ExceptItem(99).ToArray());
        CollectionAssert.AreEqual(source, source.ExceptItems(new[] { 99, 99 }).ToArray());
    }

    [TestMethod]
    public void Exclusion_RecordsUseTheirValueEquality()
    {
        var source = new[] { new RecordItem(1), new RecordItem(2) };
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItem(new RecordItem(1)).ToArray());
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItems(new[] { new RecordItem(1) }).ToArray());
    }

    [TestMethod]
    public void Exclusion_OrdinaryClassesUseAllPropertyEqualityByDefault()
    {
        var item = new PlainItem { Id = 1 };
        var source = new[] { item };
        Assert.AreEqual(0, source.ExceptItem(new PlainItem { Id = 1 }).Count());
        Assert.AreEqual(0, source.ExceptItems(new[] { new PlainItem { Id = 1 } }).Count());
        Assert.AreEqual(0, source.ExceptItem(item).Count());
        Assert.AreEqual(0, source.ExceptItems(new[] { item }).Count());
    }

    [TestMethod]
    public void Exclusion_ExplicitPropertyComparersStillWork()
    {
        var source = new[] { new PlainItem { Id = 1 }, new PlainItem { Id = 2 } };
        var excluded = new PlainItem { Id = 1 };
        var comparer = M.Simple.Expand.Extensions.CreateByProperty<PlainItem, int>(item => item.Id);
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItem(excluded, comparer).ToArray());
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItems(new[] { excluded }, comparer).ToArray());
        Assert.AreEqual(1, source.ExceptItems(new[] { excluded }, FastAllPropertiesEqualityComparer<PlainItem>.Instance).Count());
    }

    [TestMethod]
    public void ExceptItems_EnumeratesEachInputOnlyOnceAndDefersExecution()
    {
        var source = new SingleUseEnumerable<int>(new[] { 1, 2, 3 });
        var excluded = new SingleUseEnumerable<int>(new[] { 2 });
        var result = source.ExceptItems(excluded);
        Assert.AreEqual(0, source.EnumerationCount);
        Assert.AreEqual(0, excluded.EnumerationCount);
        CollectionAssert.AreEqual(new[] { 1, 3 }, result.ToArray());
        Assert.AreEqual(1, source.EnumerationCount);
        Assert.AreEqual(1, excluded.EnumerationCount);
    }

    [TestMethod]
    public void ExceptItems_ExcludesMatchingStudentsButKeepsSameIdWithDifferentFields()
    {
        var source = new[]
        {
            new StudentItem { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" },
            new StudentItem { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
            new StudentItem { Id = 3, Age = 20, Name = "WangWu", StudentCode = "S0003" },
            new StudentItem { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0004" },
            new StudentItem { Id = 5, Age = 22, Name = "LiangQi", StudentCode = "S0005" },
            new StudentItem { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" }
        };
        var excluded = new[]
        {
            new StudentItem { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
            new StudentItem { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0008" },
            new StudentItem { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" }
        };

        CollectionAssert.AreEqual(new[] { 1, 3, 4, 5 }, source.ExceptItems(excluded).Select(item => item.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, source.ExceptItem(excluded[2]).Select(item => item.Id).ToArray());
        var comparer = FastAllPropertiesEqualityComparer<StudentItem>.Instance;
        Assert.AreEqual(comparer.GetHashCode(source[1]), comparer.GetHashCode(excluded[0]));
    }

    [TestMethod]
    public void Exclusion_EntityIndexersAreSkipped()
    {
        var source = new[] { new IndexedItem { Id = 1 }, new IndexedItem { Id = 2 } };
        var excluded = new IndexedItem { Id = 1 };
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItem(excluded).ToArray());
        CollectionAssert.AreEqual(new[] { source[1] }, source.ExceptItems(new[] { excluded }).ToArray());
    }

    [TestMethod]
    public void Exclusion_ClassesWithoutReadablePropertiesUseReferenceEquality()
    {
        var item = new EmptyItem();
        Assert.AreEqual(1, new[] { item }.ExceptItem(new EmptyItem()).Count());
        Assert.AreEqual(0, new[] { item }.ExceptItems(new[] { item }).Count());
    }

    [TestMethod]
    public void Exclusion_ExplicitReferenceComparerOverridesDefaultPropertyComparison()
    {
        var item = new PlainItem { Id = 1 };
        var other = new PlainItem { Id = 1 };
        Assert.AreEqual(1, new[] { item }.ExceptItem(other, ReferenceEqualityComparer.Instance).Count());
        Assert.AreEqual(1, new[] { item }.ExceptItems(new[] { other }, ReferenceEqualityComparer.Instance).Count());
    }

    private sealed record RecordItem(int Id);
    private sealed class PlainItem { public int Id { get; set; } }
    private sealed class EmptyItem { }
    private sealed class IndexedItem
    {
        public int Id { get; set; }
        public string this[int index] => throw new InvalidOperationException("不应读取索引器。");
    }
    private sealed class StudentItem
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public string Name { get; set; }
        public string StudentCode { get; set; }
    }

    private sealed class SingleUseEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            if (++EnumerationCount > 1) throw new InvalidOperationException("数据源不能重复枚举。");
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
