# 日期与时间

格式、月份边界、日期差与区间。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- datetime
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Models;

static partial class Demo
{
    public static void Dates()
    {
        var date = new DateTime(2026, 11, 11, 10, 30, 0);
        Console.WriteLine(date.ToDefaultDate());
        Console.WriteLine(date.GetFirstDayOfMonth().ToString("yyyy-MM-dd"));
        Console.WriteLine(date.GetLastDayOfMonth().ToString("yyyy-MM-dd"));
        Console.WriteLine(date.NextDay().ToDefaultDate());
        Console.WriteLine($"相差天数：{date.DiffDays(date.AddDays(3))}");
        var periods = new List<DateTimePeriod>
        {
            new(date, date.AddHours(2)),
            new(date.AddHours(1), date.AddHours(3))
        };
        Check(periods.IsOverlap(), "时间区间重叠");
        Check(date.GetLastDayOfMonth().Day == 30, "月末");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- ToDefaultDate 用于默认日期文本；NextDay 和 PervDay 分别为后一天和前一天，PervDay 为原始 API 拼写。GetFirstDayOfMonth、GetLastDayOfMonth 返回月边界。

- DiffDays 计算日期差，includeToday 控制附加计数；DiffMonth 的算法通过 DiffMonthWayEnum 选择。CalculateAge 使用运行时当前日期。

- IsOverlap 会原地排序传入的 List，并直接读取第 0 项，所以调用前须确保非 null 且 Count > 0；建议传入副本以保护原顺序。时间区间相接（后一项开始等于前一项结束）不算重叠。

- 可空重载的回退逻辑按具体方法而异；有些使用当前时间，不要统一假设返回空字符串。DateTime 的 Kind 和时区应由调用方明确。

- IsWorkday 依据星期判断，不包含法定节假日和调休日历。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
