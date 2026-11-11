# 校验特性

手机号、邮箱与 DataAnnotations。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- validation
```

```csharp
using System.ComponentModel.DataAnnotations;
using M.Simple.Expand.Attributes;

static partial class Demo
{
    public static void Validation()
    {
        var model = new SampleContact { Email = "reader@example.com", Mobile = "13812345678" };
        var errors = new List<ValidationResult>();
        Check(Validator.TryValidateObject(model, new ValidationContext(model), errors, true), "数据注解校验");
        Console.WriteLine("邮箱与手机号 DataAnnotations 校验通过");
    }
}
public class SampleContact
{
    [Required, IsEmail] public string Email { get; set; } = "";
    [Required, IsMobile] public string Mobile { get; set; } = "";
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- 使用 System.ComponentModel.DataAnnotations.Validator.TryValidateObject 执行校验；给属性标记特性不会自动运行验证逻辑。ASP.NET Core 模型验证可按宿主约定使用。

- 常用特性包括 IsTelephone、IsMobile、IsIDcard、IsEmail、IsUrl、IsDateTime、IsInt、RegexMatch、UserId、Password。范围校验包括 GreaterThanMinValue、LessThanMaxValue、BetweenValue。

- 三个范围特性在数值解析失败时直接返回校验错误，不再将非法文本当作0。空串、纯空白、非数字、超出 decimal 范围的输入均失败；null 继续按原规则失败。合法的0仍按配置的区间判断，不会被一律拒绝。

- 范围解析沿用 decimal.TryParse 的当前文化规则；边界仍通过 IsEqualMinValue、IsEqualMaxValue 控制是否包含端点，校验错误沿用 ErrorMessage 配置。

- 必填约束请单独标记 Required。格式与范围规则沿用该版本源码的正则及转换方式，不保证覆盖所有国际号码、合法邮箱格式或证件规则。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
