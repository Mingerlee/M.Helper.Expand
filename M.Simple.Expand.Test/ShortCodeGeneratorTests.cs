using System.Collections.Concurrent;

namespace M.Simple.Expand.Test;

[TestClass]
public class ShortCodeGeneratorTests
{
    [TestMethod]
    public void GenerateShortCode_RepeatedCallsDoNotRepeatAndPreserveFormat()
    {
        var codes = Enumerable.Range(0, 10000)
            .Select(_ => ShortCodeGenerator.GenerateShortCode()).ToArray();

        Assert.AreEqual(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(codes.All(code => code.Length == 11));
        Assert.IsTrue(codes.All(code => code.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z')));
    }

    [TestMethod]
    public void GenerateShortCode_ConcurrentCallsDoNotRepeat()
    {
        var codes = new ConcurrentBag<string>();
        Parallel.For(0, 10000, _ => codes.Add(ShortCodeGenerator.GenerateShortCode()));

        Assert.AreEqual(10000, codes.Count);
        Assert.AreEqual(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }
}
