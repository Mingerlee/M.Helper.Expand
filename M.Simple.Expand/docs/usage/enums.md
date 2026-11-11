# 枚举

描述、Display、解析与下拉选项。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- enums
```

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using M.Simple.Expand;
using M.Simple.Expand.Models;

static partial class Demo
{
    public static void Enums()
    {
        Console.WriteLine(SampleStatus.Approved.GetDescription());
        Console.WriteLine(SampleStatus.Approved.GetDisplayName());
        var options = new ComboBoxIntViewModel().ToComboBoxs<SampleStatus>();
        foreach (var option in options.SelectOptions) Console.WriteLine($"{option.Value}: {option.Text}");
        Check("Approved".ToEnum<SampleStatus>() == SampleStatus.Approved, "名称解析");
        Check(SampleStatus.Approved.GetDescription() == "已通过", "描述读取");
    }
}
enum SampleStatus
{
    [Description("待审核")] Pending = 0,
    [Description("已通过"), Display(Name = "审核通过", Description = "审核已经通过")] Approved = 1
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- GetDescription 读取 DescriptionAttribute；GetDisplayName、GetDisplayDescription 读取 DisplayAttribute。当前实现未标记特性时返回空串，和部分原始 XML 注释中的名称回退描述不同。

- ToComboBoxs<T> 返回 ComboBoxIntViewModel，SelectOptions 的 Text 使用枚举名称，不是 Description，Value 使用 Int32。values 为非空集合时才启用过滤。

- ToInt32 和下拉模型将枚举值转为 int，大整数枚举可能溢出。ToEnum 解析失败返回 default，且 Enum.TryParse 可能接受没有声明的数值；需要限制范围时额外使用 Enum.IsDefined。

- GetAttributeOfType 在未定义枚举值上可能因找不到成员而异常；不要将未验证的用户输入直接用于特性读取。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
