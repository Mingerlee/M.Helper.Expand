# 字典

覆盖合并、忽略合并、弹出与删除。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- dictionary
```

```csharp
using M.Simple.Expand;

static partial class Demo
{
    public static void Dictionary()
    {
        var values = new Dictionary<string, int> { ["A"] = 1 };
        values.Merge("A", 2).Merge("B", 3);
        values.MergeIgnore("A", 99);
        Console.WriteLine($"覆盖后 A={values.GetValue("A")}");
        Console.WriteLine($"弹出 B={values.RemovePop("B")}");
        Check(values["A"] == 2 && !values.ContainsKey("B"), "覆盖、忽略与弹出");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- Merge、Merges 以新值覆盖同键数据；MergeIgnore、MergeIgnores 保留已存在键的值。方法会修改原字典，不是返回一个独立副本。

- GetValue 找不到键返回 TValue 的 default；RemovePop 先取出再删除，不存在也返回 default，不能用它区分键缺失与实际默认值。

- Merge、MergeIgnore、Merges、MergeIgnores 均支持 null 目标，但必须接收返回值（例如 target = target.Merges(source)）。批量合并遇到 null 目标时创建一个字典并保留源字典的键比较器；目标已存在时继续使用目标的比较器。source 为 null 时原样返回目标；两者都为 null 时仍返回 null。Removes 可批量删除。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
