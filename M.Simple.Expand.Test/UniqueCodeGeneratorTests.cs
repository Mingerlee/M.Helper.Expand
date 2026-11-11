using System.Collections.Concurrent;
using System.Globalization;

namespace M.Simple.Expand.Test;

[TestClass]
public class UniqueCodeGeneratorTests
{
    [TestMethod]
    public void GenerateTimeStampGuidCode_PreservesTimestampAndContainsFullGuid()
    {
        var before = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        var code = UniqueCodeGenerator.GenerateTimeStampGuidCode();
        var after = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        var timestamp = long.Parse(code[..^32], CultureInfo.InvariantCulture);
        var suffix = code[^32..];

        Assert.IsTrue(timestamp >= before && timestamp <= after);
        Assert.IsTrue(Guid.TryParseExact(suffix, "N", out var guid));
        Assert.AreNotEqual(Guid.Empty, guid);
        Assert.IsTrue(suffix.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F'));
    }

    [TestMethod]
    public void GenerateTimeStampGuidCode_RepeatedCallsDoNotRepeat()
    {
        var codes = Enumerable.Range(0, 10000)
            .Select(_ => UniqueCodeGenerator.GenerateTimeStampGuidCode()).ToArray();

        Assert.AreEqual(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(codes.Length, codes.Select(code => code[^32..]).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void GenerateTimeStampGuidCode_ConcurrentCallsDoNotRepeat()
    {
        var codes = new ConcurrentBag<string>();
        Parallel.For(0, 10000, _ => codes.Add(UniqueCodeGenerator.GenerateTimeStampGuidCode()));

        Assert.AreEqual(10000, codes.Count);
        Assert.AreEqual(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(codes.Count, codes.Select(code => code[^32..]).Distinct(StringComparer.Ordinal).Count());
    }
}
