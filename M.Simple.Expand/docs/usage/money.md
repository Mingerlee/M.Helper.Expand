# 金额

金额大写、舍入、格式化。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- money
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Enums;

static partial class Demo
{
    public static void Money()
    {
        Console.WriteLine(1234.56m.ToChinese());
        Console.WriteLine(1234.56m.ToFormatMoney(FormatMoneyEnum.C2_zh_CN));
        Console.WriteLine($"四舍五入：{1.235m.ToRound(2)}");
        Console.WriteLine($"向上取整：{1.231m.CeilingSpecial(2)}");
        Check(1.235m.ToRound(2) == 1.24m, "AwayFromZero 舍入");
        Check(((decimal?)null).ToRound(2) == 0, "可空金额");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- ToChinese 将金额转为中文大写。ToRound 使用 MidpointRounding.AwayFromZero，可空 decimal 为 null 时返回 0。

- ToFormatMoney 使用 FormatMoneyEnum。C2 等普通格式最多显示两位小数，不保证固定补零；带 zh_CN 或 en_US 的格式使用相应文化货币格式。

- CeilingSpecial 按指定小数位向上取整。MoneyStringToDecimal 为历史文本转换能力，输入格式和文化相关边界需要调用方先验证。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
