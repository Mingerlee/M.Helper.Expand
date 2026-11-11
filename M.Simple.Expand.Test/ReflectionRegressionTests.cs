using System.Reflection;
using M.Simple.Expand.Attributes;

namespace M.Simple.Expand.Test;

[TestClass]
public class ReflectionRegressionTests
{
    [DataTestMethod]
    [DataRow(nameof(Values.Byte), "255")]
    [DataRow(nameof(Values.Short), "-32768")]
    [DataRow(nameof(Values.Long), "9223372036854775807")]
    [DataRow(nameof(Values.NullableInt), "42")]
    [DataRow(nameof(Values.NullableByte), "255")]
    [DataRow(nameof(Values.Flag), "True")]
    [DataRow(nameof(Values.OptionalFlag), "False")]
    [DataRow(nameof(Values.Choice), "Friday")]
    [DataRow(nameof(Values.OptionalChoice), "Monday")]
    [DataRow(nameof(Values.Key), "d85b1407-351d-4694-9392-03acc5870eb1")]
    [DataRow(nameof(Values.OptionalDate), "2026-11-11")]
    [DataRow(nameof(Values.Duration), "01:02:03")]
    public void SetStringValue_UsesActualTargetType(string name, string text)
    {
        var model = new Values();
        var property = typeof(Values).GetProperty(name);
        property.SetStringValue(model, text);
        object expected = name switch
        {
            nameof(Values.Byte) => (byte)255,
            nameof(Values.Short) => (short)-32768,
            nameof(Values.Long) => long.MaxValue,
            nameof(Values.NullableInt) => 42,
            nameof(Values.NullableByte) => (byte)255,
            nameof(Values.Flag) => true,
            nameof(Values.OptionalFlag) => false,
            nameof(Values.Choice) => DayOfWeek.Friday,
            nameof(Values.OptionalChoice) => DayOfWeek.Monday,
            nameof(Values.Key) => new Guid("d85b1407-351d-4694-9392-03acc5870eb1"),
            nameof(Values.OptionalDate) => new DateTime(2026, 11, 11),
            nameof(Values.Duration) => new TimeSpan(1, 2, 3),
            _ => throw new InvalidOperationException(name)
        };
        Assert.AreEqual(expected, property.GetValue(model));
    }

    [DataTestMethod]
    [DataRow(nameof(Values.NullableInt))]
    [DataRow(nameof(Values.NullableByte))]
    [DataRow(nameof(Values.OptionalFlag))]
    [DataRow(nameof(Values.OptionalChoice))]
    [DataRow(nameof(Values.OptionalDate))]
    public void SetStringValue_EmptyTextClearsNullableProperty(string name)
    {
        var model = new Values();
        var property = typeof(Values).GetProperty(name);
        property.SetStringValue(model, " ");
        Assert.IsNull(property.GetValue(model));
    }

    [DataTestMethod]
    [DataRow(nameof(Values.Byte), "256")]
    [DataRow(nameof(Values.Short), "32768")]
    [DataRow(nameof(Values.NullableInt), "invalid")]
    [DataRow(nameof(Values.OptionalDate), "invalid")]
    [DataRow(nameof(Values.Time), "")]
    [DataRow(nameof(Values.Time), "invalid")]
    public void SetStringValue_InvalidInputDoesNotOverwriteOriginal(string name, string text)
    {
        var model = new Values();
        var property = typeof(Values).GetProperty(name);
        object before = property.GetValue(model);
        bool failed = false;
        try { property.SetStringValue(model, text); }
        catch (Exception) { failed = true; }
        Assert.IsTrue(failed);
        Assert.AreEqual(before, property.GetValue(model));
    }

    [TestMethod]
    public void SetStringValue_StringNullAndEmptyRemainDistinct()
    {
        var model = new Values();
        var property = typeof(Values).GetProperty(nameof(Values.Text));
        property.SetStringValue(model, "");
        Assert.AreEqual("", model.Text);
        property.SetStringValue(model, null);
        Assert.IsNull(model.Text);
        Assert.ThrowsException<ArgumentException>(() => typeof(Values).GetProperty("ReadOnly").SetStringValue(model, "x"));
    }

    [DataTestMethod]
    [DataRow(nameof(Values.Single))]
    [DataRow(nameof(Values.OptionalSingle))]
    [DataRow(nameof(Values.Double))]
    [DataRow(nameof(Values.OptionalDouble))]
    [DataRow(nameof(Values.Decimal))]
    public void GetStringValue_FormatsEachFloatingTypeWithoutInvalidCast(string name)
    {
        var model = new Values();
        var property = typeof(Values).GetProperty(name);
        var expected = ((IFormattable)property.GetValue(model)).ToString("0.0000", null);
        Assert.AreEqual(expected, property.GetStringValue(model));
        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        {
            property.SetValue(model, null);
            Assert.AreEqual("", property.GetStringValue(model));
        }
    }

    [TestMethod]
    public void ModifyField_AssignsNullableAndSmallIntegralProperties()
    {
        var target = new Values();
        target.ModifyField(new { NullableInt = 123, NullableByte = (byte)255, Byte = (byte)128, Short = (short)-7 });
        Assert.AreEqual(123, target.NullableInt);
        Assert.AreEqual((byte)255, target.NullableByte);
        Assert.AreEqual((byte)128, target.Byte);
        Assert.AreEqual((short)-7, target.Short);
        target.Text = "";
        target.ModifyField(new { Text = (string)null, NullableInt = (int?)null });
        Assert.IsNull(target.Text);
        Assert.IsNull(target.NullableInt);
    }

    [TestMethod]
    public void CompareModel_DetectsObjectTypeNullEmptyAndSubsecondChanges()
    {
        var before = new CompareValues { Text = null, Boxed = 1, Time = new DateTime(2026, 11, 11) };
        var after = new CompareValues { Text = "", Boxed = "1", Time = before.Time.AddTicks(1) };
        var changes = after.CompareModel(before);
        CollectionAssert.AreEquivalent(new[] { "Text", "Boxed", "Time" }, changes.Select(c => c.FiledName).ToArray());
        var text = changes.Single(c => c.FiledName == "Text");
        Assert.IsNull(text.FiledOldValue);
        Assert.AreEqual("", text.FiledNewValue);
        Assert.IsTrue(changes.All(c => c.IsModify == true));
    }

    [TestMethod]
    public void CompareModel_RespectsAttributeFilterAndValueEquality()
    {
        var before = new CompareValues { Boxed = 1m, Text = "old" };
        var after = new CompareValues { Boxed = 1.00m, Text = "new" };
        Assert.AreEqual(0, after.CompareModel(before, true).Count);
        Assert.AreEqual(1, after.CompareModel(before).Count);
        after.Boxed = 2m;
        Assert.AreEqual("Boxed", after.CompareModel(before, true).Single().FiledName);
    }

    [TestMethod]
    public void CompareModel_ReportsAdditionAndDeletionAndSkipsIndexers()
    {
        var model = new CompareValues();
        var added = model.CompareModel((CompareValues)null);
        var deleted = ((CompareValues)null).CompareModel(model);
        Assert.AreEqual(4, added.Count);
        Assert.IsTrue(added.All(c => c.IsAdded == true));
        Assert.AreEqual(4, deleted.Count);
        Assert.IsTrue(deleted.All(c => c.IsDelete == true));
        Assert.IsTrue(((CompareValues)null).CompareModel(null).Count == 0);
        Assert.AreEqual(0, model.CompareModel(model).Count);
    }

    public class Values
    {
        public byte Byte { get; set; } = 1;
        public short Short { get; set; } = 2;
        public long Long { get; set; }
        public int? NullableInt { get; set; } = 3;
        public byte? NullableByte { get; set; } = 4;
        public bool Flag { get; set; }
        public bool? OptionalFlag { get; set; } = true;
        public DayOfWeek Choice { get; set; }
        public DayOfWeek? OptionalChoice { get; set; } = DayOfWeek.Tuesday;
        public Guid Key { get; set; }
        public DateTime? OptionalDate { get; set; } = new DateTime(2020, 1, 1);
        public TimeSpan Duration { get; set; }
        public DateTime Time { get; set; } = new DateTime(2026, 11, 11);
        public string Text { get; set; }
        public string ReadOnly => "readonly";
        public float Single { get; set; } = 1.25f;
        public float? OptionalSingle { get; set; } = -2.5f;
        public double Double { get; set; } = 1.25;
        public double? OptionalDouble { get; set; } = double.MaxValue;
        public decimal Decimal { get; set; } = 3.75m;
    }

    public class CompareValues
    {
        public string Text { get; set; }
        [CompareValue] public object Boxed { get; set; }
        public DateTime Time { get; set; }
        public string ReadOnly => "same";
        public string this[int index] => throw new InvalidOperationException("不能读取索引器");
        public static string Static => throw new InvalidOperationException("不能读取静态属性");
    }
}
