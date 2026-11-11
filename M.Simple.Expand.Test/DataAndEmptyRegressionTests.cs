using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using M.Simple.Expand.Models;
using Microsoft.Extensions.Configuration;

namespace M.Simple.Expand.Test;

[TestClass]
public class DataAndEmptyRegressionTests
{
    [TestMethod]
    public void DataTable_RoundTripsDisplayNamesNullsEnumsAndReadOnlyProperties()
    {
        IList source = new List<Row>
        {
            new() { Id = 1, Name = "甲", Count = 9, Status = DayOfWeek.Friday, OptionalStatus = DayOfWeek.Monday },
            new() { Id = 2, Name = null, Count = null, Status = DayOfWeek.Sunday, OptionalStatus = null }
        };
        using var table = source.ToDataTable();
        Assert.IsTrue(table.Columns.Contains("名称"));
        Assert.AreEqual(7, table.Columns.Count);
        Assert.IsTrue(table.Rows.Cast<DataRow>().All(row => row.RowState == DataRowState.Unchanged));
        Assert.AreEqual("Name", table.Columns["名称"].ExtendedProperties["M.Simple.PropertyName"]);
        var restored = table.Where(_ => true).ToLists<Row>();
        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual("甲", restored[0].Name);
        Assert.AreEqual(9, restored[0].Count);
        Assert.AreEqual(DayOfWeek.Friday, restored[0].Status);
        Assert.AreEqual(DayOfWeek.Monday, restored[0].OptionalStatus);
        Assert.IsNull(restored[1].Name);
        Assert.IsNull(restored[1].Count);
        Assert.IsNull(restored[1].OptionalStatus);
        CollectionAssert.AreEqual(new[] { 1, 2 }, restored.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void DataTable_DuplicateDisplayNamesDoNotLoseValues()
    {
        IList source = new List<DuplicateNames> { new() { First = "first", Second = "second", Third = "third" } };
        using var table = source.ToDataTable();
        var restored = table.ToLists<DuplicateNames>().Single();
        Assert.AreEqual(3, table.Columns.Count);
        Assert.AreEqual("first", restored.First);
        Assert.AreEqual("second", restored.Second);
        Assert.AreEqual("third", restored.Third);
    }

    [TestMethod]
    public void ToLists_AcceptsPropertyNamesDisplayNamesAndCompatibleScalarTypes()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(long));
        table.Columns.Add("名称", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("Count", typeof(long));
        table.Rows.Add(8L, "手工表", "Friday", 12L);
        var restored = table.ToLists<Row>().Single();
        Assert.AreEqual(8, restored.Id);
        Assert.AreEqual("手工表", restored.Name);
        Assert.AreEqual(12, restored.Count);
        Assert.AreEqual(DayOfWeek.Friday, restored.Status);
    }

    [TestMethod]
    public void DataTable_NullAndEmptyInputsReturnEmptyResultsAndPreserveKnownSchema()
    {
        DataTable missing = null;
        Assert.AreEqual(0, missing.ToLists<Row>().Count);
        using var missingFilter = missing.Where(_ => true);
        Assert.AreEqual(0, missingFilter.Rows.Count);
        using var table = ((IList)new List<Row>()).ToDataTable();
        Assert.AreEqual(7, table.Columns.Count);
        using var clone = table.Where(_ => true);
        Assert.AreEqual(table.Columns.Count, clone.Columns.Count);
        Assert.AreEqual(0, clone.Rows.Count);
        Assert.AreEqual(0, clone.ToLists<Row>().Count);
        Assert.AreEqual(0, ((IList)null).ToDataTable().Rows.Count);
        Assert.AreEqual(0, new ArrayList().ToDataTable().Columns.Count);
        Assert.ThrowsException<ArgumentNullException>(() => table.Where(null));
        Assert.ThrowsException<ArgumentNullException>(() => missing.Where(null));
    }

    [TestMethod]
    public void IsOverlap_EmptyNullAndSinglePeriodsReturnFalse()
    {
        Assert.IsFalse(new List<DateTimePeriod>().IsOverlap());
        Assert.IsFalse(new List<DatePeriod>().IsOverlap());
        Assert.IsFalse(((List<DateTimePeriod>)null).IsOverlap());
        Assert.IsFalse(((List<DatePeriod>)null).IsOverlap());
        var today = new DateTime(2026, 11, 11);
        Assert.IsFalse(new List<DateTimePeriod> { new(today, today.AddHours(1)) }.IsOverlap());
        Assert.IsFalse(new List<DatePeriod> { new(today, today.AddDays(1)) }.IsOverlap());
        Assert.IsTrue(new List<DateTimePeriod> { new(today, today.AddHours(2)), new(today.AddHours(1), today.AddHours(3)) }.IsOverlap());
        Assert.IsTrue(new List<DatePeriod> { new(today, today.AddDays(2)), new(today.AddDays(1), today.AddDays(3)) }.IsOverlap());
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DictionaryMerge_NullTargetRetainsAllEntriesAndComparer(bool ignoreExisting)
    {
        Dictionary<string, int> missing = null;
        var source = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1, ["B"] = 2 };
        var merged = ignoreExisting ? missing.MergeIgnores(source) : missing.Merges(source);
        Assert.AreEqual(2, merged.Count);
        Assert.AreEqual(1, merged["a"]);
        Assert.IsNull(missing); // 调用方需接收返回值。
        merged["a"] = 3;
        var result = ignoreExisting ? merged.MergeIgnores(source) : merged.Merges(source);
        Assert.AreSame(merged, result);
        Assert.AreEqual(ignoreExisting ? 3 : 1, result["A"]);
        Assert.AreEqual(1, source["A"]);
        Assert.AreSame(merged, ignoreExisting ? merged.MergeIgnores(null) : merged.Merges(null));
        Assert.IsNotNull(ignoreExisting ? missing.MergeIgnores(new Dictionary<string, int>()) : missing.Merges(new Dictionary<string, int>()));
    }

    [TestMethod]
    [DoNotParallelize]
    public void ConfigExist_DistinguishesMissingValuesAndParentSections()
    {
        // 注入内存配置，避免读取真实appsettings或修改进程工作目录。
        var instanceField = typeof(AppSetting).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        object original = instanceField.GetValue(null);
        var instance = (AppSetting)RuntimeHelpers.GetUninitializedObject(typeof(AppSetting));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Text"] = "value",
            ["Empty"] = "",
            ["Parent:Child"] = "value"
        }).Build();
        typeof(AppSetting).GetField("<Config>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, config);
        try
        {
            instanceField.SetValue(null, instance);
            Assert.IsFalse(AppSetting.ConfigExist("Missing"));
            Assert.IsFalse(AppSetting.ConfigExist("Parent:Missing"));
            Assert.IsTrue(AppSetting.ConfigExist("Text"));
            Assert.IsTrue(AppSetting.ConfigExist("Empty"));
            Assert.IsTrue(AppSetting.ConfigExist("Parent"));
            Assert.IsTrue(AppSetting.ConfigExist("Parent:Child"));
        }
        finally
        {
            instanceField.SetValue(null, original);
            (config as IDisposable)?.Dispose();
        }
    }

    public class Row
    {
        public int Id { get; set; }
        [DisplayName("名称")] public string Name { get; set; } = "initial";
        public int? Count { get; set; } = 99;
        public DayOfWeek Status { get; set; }
        public DayOfWeek? OptionalStatus { get; set; } = DayOfWeek.Tuesday;
        public string ReadOnly => "readonly";
        public string PrivateSetter { get; private set; } = "private";
        public string this[int index] => throw new InvalidOperationException("索引器不能映射");
    }

    public class DuplicateNames
    {
        [DisplayName("Second")] public string First { get; set; }
        [DisplayName("Second")] public string Second { get; set; }
        [DisplayName("Second")] public string Third { get; set; }
    }
}
