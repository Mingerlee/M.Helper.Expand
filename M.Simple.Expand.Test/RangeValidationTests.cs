using System.ComponentModel.DataAnnotations;
using M.Simple.Expand.Attributes;

namespace M.Simple.Expand.Test;

[TestClass]
public class RangeValidationTests
{
    [DataTestMethod]
    [DataRow("not-a-number")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("NaN")]
    [DataRow("Infinity")]
    [DataRow("79228162514264337593543950336")]
    [DataRow("-79228162514264337593543950336")]
    [DataRow(null)]
    public void RangeAttributes_RejectUnparseableOrNullInput(string value)
    {
        foreach (var attribute in CreateRangesContainingZero())
        {
            var result = attribute.GetValidationResult(value, new ValidationContext(new object()));
            Assert.IsNotNull(result, attribute.GetType().Name + " 不应将非法输入当作0通过校验。");
        }
    }

    [TestMethod]
    public void GreaterThanMinValue_PreservesInclusiveAndExclusiveBoundaries()
    {
        var exclusive = new GreaterThanMinValue { MinValue = 0 };
        var inclusive = new GreaterThanMinValue { MinValue = 0, IsEqualMinValue = true };
        Assert.IsFalse(IsValid(exclusive, -0.5m));
        Assert.IsFalse(IsValid(inclusive, -0.5m));
        Assert.IsFalse(IsValid(exclusive, 0));
        Assert.IsTrue(IsValid(inclusive, 0));
        Assert.IsTrue(IsValid(exclusive, 0.5m));
        Assert.IsTrue(IsValid(inclusive, 0.5m));
    }

    [TestMethod]
    public void LessThanMaxValue_PreservesInclusiveAndExclusiveBoundaries()
    {
        var exclusive = new LessThanMaxValue { MaxValue = 0 };
        var inclusive = new LessThanMaxValue { MaxValue = 0, IsEqualMaxValue = true };
        Assert.IsTrue(IsValid(exclusive, -0.5m));
        Assert.IsTrue(IsValid(inclusive, -0.5m));
        Assert.IsFalse(IsValid(exclusive, 0));
        Assert.IsTrue(IsValid(inclusive, 0));
        Assert.IsFalse(IsValid(exclusive, 0.5m));
        Assert.IsFalse(IsValid(inclusive, 0.5m));
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void BetweenValue_PreservesAllBoundaryCombinations(bool includeMin, bool includeMax)
    {
        var attribute = new BetweenValue
        {
            MinValue = -1,
            MaxValue = 1,
            IsEqualMinValue = includeMin,
            IsEqualMaxValue = includeMax
        };
        Assert.IsFalse(IsValid(attribute, -2));
        Assert.AreEqual(includeMin, IsValid(attribute, -1));
        Assert.IsTrue(IsValid(attribute, 0));
        Assert.IsTrue(IsValid(attribute, -0.5m));
        Assert.IsTrue(IsValid(attribute, 0.5m));
        Assert.AreEqual(includeMax, IsValid(attribute, 1));
        Assert.IsFalse(IsValid(attribute, 2));
    }

    [TestMethod]
    public void RangeAttributes_ContinueAcceptingValidZeroAndNumericStrings()
    {
        foreach (var attribute in CreateRangesContainingZero())
        {
            Assert.IsTrue(IsValid(attribute, 0));
            Assert.IsTrue(IsValid(attribute, 0m));
            Assert.IsTrue(IsValid(attribute, "0"));
        }
    }

    [TestMethod]
    public void RangeAttributes_ReturnConfiguredErrorMessageForInvalidText()
    {
        foreach (var attribute in CreateRangesContainingZero())
        {
            attribute.ErrorMessage = "{0}必须是有效数值";
            var context = new ValidationContext(new object()) { DisplayName = "金额" };
            var result = attribute.GetValidationResult("invalid", context);
            Assert.IsNotNull(result);
            Assert.AreEqual("金额必须是有效数值", result.ErrorMessage);
        }
    }

    [TestMethod]
    public void TryValidateObject_ReportsErrorsForAllThreeRangeAttributes()
    {
        var model = new InvalidRangeModel();
        var errors = new List<ValidationResult>();
        Assert.IsFalse(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.AreEqual(3, errors.Count);
    }

    private static bool IsValid(ValidationAttribute attribute, object value)
    {
        return attribute.GetValidationResult(value, new ValidationContext(new object())) == ValidationResult.Success;
    }

    // 所有区间均包含0，确保能捕获“解析失败回退0而误通过”的回归。
    private static ValidationAttribute[] CreateRangesContainingZero()
    {
        return new ValidationAttribute[]
        {
            new GreaterThanMinValue { MinValue = -1 },
            new GreaterThanMinValue { MinValue = 0, IsEqualMinValue = true },
            new LessThanMaxValue { MaxValue = 1 },
            new LessThanMaxValue { MaxValue = 0, IsEqualMaxValue = true },
            new BetweenValue { MinValue = -1, MaxValue = 1 },
            new BetweenValue { MinValue = -1, MaxValue = 1, IsEqualMinValue = true },
            new BetweenValue { MinValue = -1, MaxValue = 1, IsEqualMaxValue = true },
            new BetweenValue { MinValue = -1, MaxValue = 1, IsEqualMinValue = true, IsEqualMaxValue = true }
        };
    }

    private sealed class InvalidRangeModel
    {
        [GreaterThanMinValue(MinValue = -1)] public string Lower { get; set; } = "invalid";
        [LessThanMaxValue(MaxValue = 1)] public string Upper { get; set; } = "invalid";
        [BetweenValue(MinValue = -1, MaxValue = 1)] public string Between { get; set; } = "invalid";
    }
}
