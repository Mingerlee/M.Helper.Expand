# DataTable

列表转表、表转模型与筛选。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- datatable
```

```csharp
using System.Collections;
using M.Simple.Expand;

static partial class Demo
{
    public static void DataTable()
    {
        IList rows = new List<SampleRow> { new() { Id = 1, Name = "示例" } };
        var table = rows.ToDataTable();
        var restored = table.ToLists<SampleRow>();
        Console.WriteLine($"列数：{table.Columns.Count}；行数：{table.Rows.Count}");
        Console.WriteLine($"恢复：{restored[0].Id} / {restored[0].Name}");
        Check(restored[0].Name == "示例", "DataTable 往返");
    }
}
public class SampleRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- IList.ToDataTable 将公共可读实例属性投影到 DataTable，跳过索引器。保留 DisplayName 显示列名，并在每列 ExtendedProperties["M.Simple.PropertyName"] 记录原属性名；重复显示名会生成独立列。ToLists<T> 优先使用该映射，还支持普通属性名列和手工创建的 DisplayName 列；T 必须有无参构造，仅写入公共可写的非索引属性。

- ToLists 支持兼容标量转换、可空类型、枚举及 Guid。DBNull 会还原为可空或引用属性的 null，非可空值类型保留构造初始值。复杂模型建议显式映射；如果自行移除映射信息并修改列名，还原将依赖列名匹配。

- Where 返回克隆结构的新表，导入匹配行；源为 null 时返回无结构空表，空表保留结构，筛选条件为 null 时始终抛 ArgumentNullException。ToLists 的 null/空表返回空列表。泛型空列表 ToDataTable 保留可推断的属性结构；null 或无法推断元素类型的非泛型空列表返回无结构空表。

- IsNullOrEmpty 同时检查列和行，无列或无行均视为空。示例验证基本属性的往返转换。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
