# 实体比较与赋值

新旧字段差异、特性筛选与属性赋值。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

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

- isAttr=true 时只处理 CompareValue 标记的属性；默认比较泛型 T 声明的所有公共可读实例属性，跳过索引器。直接按属性值的 Equals 判断，包含 object 属性，区分 null 与空字符串，也能检测 DateTime 的细微精度变化。引用类型沿用其 Equals（未重写时按引用比较），不递归比较对象图或集合内容；返回值文本仅用于展示，不作为相等判断依据。

- 新对象为 null、旧对象非 null 时将字段标记为删除（IsDelete）；旧对象为 null 时标记为新增（IsAdded）；两者都为 null 时无差异。IsModify 方法只根据差异条数判断，因此新增、删除也返回 true。

- ModifyField 会直接修改接收对象，isAttr 可结合 ModifyValueAttribute 选择及映射属性；它不会自动持久化。

- SetStringValue 按实际目标类型及当前文化转换，支持基础数值、bool、DateTime、枚举、Guid、TimeSpan 及其可空形式。可空值类型的 null/空白文本用于清空；字符串区分 null 和空串。非法或溢出输入抛出转换异常且不覆盖原值，不能写入的属性、不支持的类型也显式报错；不再将非法输入静默替换成0或当前日期。

- GetStringValue 对 decimal、float、double（含可空类型）按各自类型格式化为四位小数，避免装箱类型强转异常；null 仍返回空串。它是展示函数，不是可逆序列化器。ModifyField 跳过只读属性和索引器，支持可空值赋值及清空。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
