# 日期与时间

格式、月份边界、日期差与区间。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- datetime
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Enums;
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
        var monthEnd = new DateTime(2026, 1, 31);
        var nextMonth = new DateTime(2026, 2, 28);
        Console.WriteLine($"整月差：{monthEnd.DiffMonth(nextMonth, DiffMonthWayEnum.精确)}；反向：{nextMonth.DiffMonth(monthEnd, DiffMonthWayEnum.精确)}");
        Check(monthEnd.DiffMonth(nextMonth, DiffMonthWayEnum.精确) == 1, "月末完整月边界");
        Check(nextMonth.DiffMonth(monthEnd, DiffMonthWayEnum.精确) == -1, "月份差方向");
        Check(date.DiffMonth(date, DiffMonthWayEnum.跨月) == 0, "同日不跨月");
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

- start.DiffDays(end) 与 start.DiffMonth(end) 均忽略时分秒，方向按结束日期减开始日期：向未来为正，向过去为负。DiffDays 的 includeToday 保持原规则，在日期差上直接加1（不按方向调整）；默认 false 时同日为0。CalculateAge 使用运行时当前日期。

- DiffMonth 的“简单”和“跨月”均统计跨过的自然月边界数，与月份编号之差一致：同月为0，1月31日到2月1日为1。两个枚举保留兼容，均不计算“涉及几个月”，也不向不足一个月的同月区间加1。

- “精确”计算完整月数：先取较早、较晚日期，再找最大的非负 n，使较早日期.AddMonths(n)不超过较晚日期，最后应用方向。月份没有对应日时按 AddMonths 取月末；从原始日期直接加 n 个月，不逐月累加，也不约定所有月末互相等价。因此非闰年1月31日至2月28日为1，2月28日至3月28日为1，4月30日至5月29日为0。

- 三种模式同日均返回0，交换起止日期结果取反。无效 DiffMonthWayEnum 值抛 ArgumentOutOfRangeException。兼容性变化：DiffMonth 修正了旧版相反的方向、跨月多算1和整月边界问题；依赖旧结果符号或额外加1的调用方需要调整。

以下示例均从前一日期算到后一日期，反向调用结果取反：

| 开始 → 结束 | 简单 / 跨月 | 精确整月 |
| --- | ---: | ---: |
| 2026-01-15 → 2026-01-15 | 0 | 0 |
| 2026-01-01 → 2026-01-31 | 0 | 0 |
| 2026-01-31 → 2026-02-01 | 1 | 0 |
| 2026-01-31 → 2026-02-28 | 1 | 1 |
| 2024-01-31 → 2024-02-28 | 1 | 0 |
| 2024-01-31 → 2024-02-29 | 1 | 1 |
| 2026-01-31 → 2026-03-30 | 2 | 1 |
| 2025-12-31 → 2026-01-01 | 1 | 0 |

- 两种 IsOverlap 在 null、空列表或仅一个区间时均返回 false。多个区间仍会原地排序，建议传入副本以保护原顺序。DateTimePeriod 的端点相接不算重叠；DatePeriod 按日期比较，端点相接算重叠，沿用原有边界规则。

- 可空重载的回退逻辑按具体方法而异；有些使用当前时间，不要统一假设返回空字符串。DateTime 的 Kind 和时区应由调用方明确。

- IsWorkday 依据星期判断，不包含法定节假日和调休日历。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
