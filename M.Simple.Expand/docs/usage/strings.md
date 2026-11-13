# 字符串

遮盖、命名转换、全半角、匹配。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- strings
```

```csharp
using M.Simple.Expand;

static partial class Demo
{
    public static void Strings()
    {
        Console.WriteLine("13812345678".ToCover());
        Console.WriteLine("user_name".UnderlineToCamel());
        Console.WriteLine("UserName".CamelToUnderline());
        Console.WriteLine("ＡＢＣ１２３".ToDBC());
        Console.WriteLine("abc".ReverseString());
        Check("13812345678".ToCover() == "138****5678", "手机号遮盖");
        Check("".GetValueOrDefault("默认") == "默认", "空值替代");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- 导入 M.Simple.Expand。ToCover 按长度选择保留位数：1 位原样返回，2 位保留首位，3/4 位保留首尾，5–10 位保留前后各 2 位，超过 10 位保留前 3 后 4 位。null 或空串返回空串。

- GetValueOrDefault 为 null 或空串提供默认值，不替代纯空白文本；UnderlineToCamel、CamelToUnderline 用于命名转换；ReverseString、SwapCase 用于字符处理。

- GetRealLength 使用源码的字符计数规则；ToHashCode 用于进程内哈希用途，不应当作稳定的跨进程唯一 ID。

- ContainsAny 和 ContainsAll 支持 StringComparison 重载。多邮箱字符串可用 GetEMailAddressArray 拆分。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
