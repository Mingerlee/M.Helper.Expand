# 实体比较与赋值

新旧字段差异、特性筛选与属性赋值。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- comparison
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Attributes;

static partial class Demo
{
    public static void Comparison()
    {
        var before = new CompareItem { Name = "旧名称", Amount = 100m };
        var after = new CompareItem { Name = "新名称", Amount = 120m };
        // 接收者是新模型，参数是旧模型；true 表示只比较有 CompareValue 标记的属性。
        var differences = after.CompareModel(before, isAttr: true);
        foreach (var diff in differences)
            Console.WriteLine($"{diff.FiledName}: {diff.FiledOldValue} → {diff.FiledNewValue}");
        Check(differences.Count == 2 && after.IsModify(before, true), "字段差异");
    }
}
public class CompareItem
{
    [CompareValue] public string Name { get; set; } = "";
    [CompareValue] public decimal Amount { get; set; }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- 调用方向为 newModel.CompareModel(oldModel)，不要颠倒。结果为 ComparesResults，属性沿用 FiledName、FiledOldValue、FiledNewValue 的历史拼写。

- isAttr=true 时只处理 CompareValue 标记的字段；默认比较值类型和 string 属性，以 ToString 文本比较，不是深比较。声明为 object 的属性会被过滤；null 属性值与空文本可能被视为一致。

- 新对象为 null 时返回空差异；旧对象为 null 时将可比较字段标记为新增。IsModify 只根据差异条数判断。

- ModifyField 会直接修改接收对象，isAttr 可结合 ModifyValueAttribute 选择及映射属性；它不会自动持久化。

- SetStringValue、GetStringValue 是历史反射转换函数，可空类型及 float/double 存在实现边界；不要把它当作任意类型的序列化器。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
