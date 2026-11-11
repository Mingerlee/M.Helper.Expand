# 集合与比较器

批次、排除、分组与按属性去重。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- collections
```

```csharp
using M.Simple.Expand;

static partial class Demo
{
    public static void Collections()
    {
        var values = new[] { 1, 2, 3, 4, 5 };
        Console.WriteLine("分批：" + string.Join(" / ", values.SplitBatch(2).Select(batch => string.Join(",", batch))));
        // 基础值类型显式使用默认相等比较器，避免全属性比较器没有可比较属性。
        var remaining = values.ExceptItems(new[] { 2, 4 }, EqualityComparer<int>.Default).ToArray();
        Console.WriteLine("排除：" + string.Join(",", remaining));
        Check(remaining.SequenceEqual(new[] { 1, 3, 5 }), "基础类型排除比较器");
        // 与 LINQ 的 TakeLast 同名时，使用完整静态类型避免歧义。
        Console.WriteLine("末尾：" + string.Join(",", M.Simple.Expand.Extensions.TakeLast(values, 2)));
        var rows = new[] { new SampleRow { Id = 1, Name = "A" }, new SampleRow { Id = 1, Name = "B" } };
        var comparer = M.Simple.Expand.Extensions.CreateByProperty<SampleRow, int>(row => row.Id);
        Check(rows.Distinct(comparer).Count() == 1, "按属性去重");
        Check(values.SplitBatch(2).Count() == 3, "尾批保留");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- SplitBatch 按 batchSize 分批并保留尾批；batchSize < 1 或空来源直接返回空结果，不抛非法批次异常。它是延迟执行方法，结果枚举时才处理输入。

- ExceptItem、ExceptItems 默认使用全属性比较器，不是 EqualityComparer<T>.Default。对 int 等没有公共属性的类型会把不同值视为相等；请像示例一样显式传 EqualityComparer<int>.Default。GroupBySelectFirst、GroupByThenSortTakeFirst 处理分组取首条；WhereNotNull、Flatten 处理过滤及扁平化。

- CreateByProperty<T,TKey> 创建基于键的比较器，可用于 Distinct；FastAllPropertiesEqualityComparer<T>.Instance 比较全部公共实例属性，包含表达式预编译，不递归深比较引用对象。

- TakeLast 与 System.Linq.Enumerable.TakeLast 同名，示例使用 M.Simple.Expand.Extensions.TakeLast 显式调用。

- Shuffle、RandomTake 为随机行为，不承诺加密强度。ToDictionarySafe 重复键取最后一个值，ToDictionarys 重复键保留第一个值；这些规则不能当作数据库唯一约束。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
