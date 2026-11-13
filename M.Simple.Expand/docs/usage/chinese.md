# 中文与拼音

拼音、首字母、中文日期与农历。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- chinese
```

```csharp
using M.Simple.Expand;

static partial class Demo
{
    public static void Chinese()
    {
        Console.WriteLine("中国".ToPinYin());
        Console.WriteLine("中国".ToFirstPinyin());
        Console.WriteLine(new DateTime(2026, 11, 11).ToChineseDateString());
        Console.WriteLine(new DateTime(2026, 11, 11).ToLunarCalendarDate());
        Check(!string.IsNullOrWhiteSpace("中国".ToPinYin()), "拼音转换");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- 拼音由 PinYinConverterCore 提供；ToPinYin 返回拼音，ToFirstPinyin 返回首字母。多音字结果由库和实现决定，不等同于人名或地名语义识别。

- ToChineseDateString、ToChineseDateTimeString 提供中文日期显示，ToLunarCalendarDate 调用 ChineseCalendarHelper。

- 农历辅助实现存在日期范围边界，超出支持区间时可能异常。示例固定使用 2026-11-11，不依赖运行当天。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
