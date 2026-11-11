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

- IList.ToDataTable 将列表的属性投影到 DataTable；DataTable.ToLists<T> 按同名列给公共属性赋值，T 必须有无参构造。

- ToLists 不做通用类型转换，只跳过 DBNull；列值与属性类型必须兼容，目标属性需可写。复杂模型建议显式映射。

- Where 返回克隆结构的新表，导入匹配行。当前源码在 dt 为 null 时仍调用 dt.Clone，调用前须确保非 null。空表可正常得到空克隆。

- IsNullOrEmpty 同时检查列和行，无列或无行均视为空。示例验证基本属性的往返转换。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
