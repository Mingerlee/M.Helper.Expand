# 反射扩展

以下为新增源码 API，导入 `using M.Simple.Expand;`。尚未单独发布包含这些 API 的新版本，开发期间请使用项目引用。

## 常用 API

| 方法 | 用途 |
|---|---|
| `GetPropertyOrNull` / `GetFieldOrNull` | 按名称查找成员，不存在时返回 null |
| `GetMethodOrNull` | 按名称和参数类型查找方法 |
| `GetPropertyValue<T>` / `SetPropertyValue` | 按属性名称读取、赋值 |
| `GetAttribute<T>` / `GetAttributes<T>` | 读取成员上的一个或多个特性 |
| `IsPublicReadable` / `IsPublicWritable` | 检查公开实例访问器，排除索引器 |
| `GetMemberDisplayName` | 依次读取 Display.Name、DisplayName、Description，最后使用成员名称 |
| `ToPropertyDictionary` | 将公开可读属性浅拷贝到字典 |
| `IsSimpleType` | 判断标量类型，支持 Nullable、枚举、日期、Guid 等 |
| `IsNullableType` / `GetNonNullableType` | 判断 Nullable<T>、获取其底层类型 |
| `GetGenericBaseType` / `GetGenericInterfaces` | 查找匹配泛型定义的基类和接口 |
| `GetLoadableTypes` / `GetImplementations<TContract>` | 获取可加载类型、扫描公开具体实现类 |

## 示例

```csharp
using System.ComponentModel;
using M.Simple.Expand;

var product = new Product();
product.SetPropertyValue("Quantity", "12");
int? quantity = product.GetPropertyValue<int?>("Quantity"); // 12
var property = typeof(Product).GetPropertyOrNull("Name");
string title = property!.GetMemberDisplayName(); // 商品名称
var values = product.ToPropertyDictionary(ignoreNull: true,
    excludedProperties: ["InternalCode"]);

bool simple = typeof(int?).IsSimpleType(); // true
Type underlying = typeof(int?).GetNonNullableType(); // typeof(int)
var interfaces = typeof(List<int>).GetGenericInterfaces(typeof(IEnumerable<>));
// 返回 IEnumerable<int>；同一泛型接口有多种构造类型时全部保留。

public sealed class Product
{
    [DisplayName("商品名称")]
    public string Name { get; set; } = "示例商品";
    public int? Quantity { get; set; }
    public string? InternalCode { get; set; }
}
```

## 行为约定

- 成员查找默认区分大小写，只查找公开实例成员（包括继承成员）。可通过 BindingFlags 显式调整；不存在才返回 null，参数错误和重名歧义不会被吞掉。方法查找使用 ExactBinding，不提供泛型方法动态调用或重载推断。
- 属性读写限定公开实例属性，排除索引器；读取要求公开 getter，赋值要求公开 setter。找不到或访问器不符合条件时抛 MissingMemberException。不会绕过私有 setter。
- `GetPropertyValue<T>` 返回原始值并直接强制转换；不自动做数字转换。读取 null 时，请使用可空值类型或引用类型；使用非可空值类型会抛异常，不会静默返回 0/false。
- `SetPropertyValue` 优先保留可直接赋值的值。不能直接赋值的字符串沿用 SetStringValue 的转换规则与当前文化设置（包括 Guid、枚举、可空值类型）；其他类型不做隐式数值转换。转换失败不覆盖原值；自定义 getter/setter 抛出的异常正常传播。
- `GetAttribute` / `GetAttributes` 的 inherit 默认 true，继承及 AttributeUsage 规则沿用 .NET。单特性 API 遇到多个同类特性会报告歧义，可用列表 API 读取全部。
- 字典转换是浅拷贝，保留复杂属性对象引用；默认保留 null，忽略静态属性、索引器、不可公开读取的属性。排除名单使用原始属性名且区分大小写。显示名称相同会报错，不覆盖已有键；不要将其直接当作日志脱敏或递归序列化工具。
- `IsNullableType` 只判断 Nullable<T>，不分析 string? 等可空引用类型注解。IsSimpleType 包含原始类型（排除 void）、枚举、string、decimal、DateTime、DateTimeOffset、TimeSpan、Guid、DateOnly、TimeOnly，不包含集合和复杂对象。
- 泛型查找必须传 `typeof(IEnumerable<>)` 等泛型定义，基类查找包括类型自身；接口查找包括接口自身，返回多个匹配构造类型。
- 程序集扫描不实例化类型，也不自动加载其他程序集。实现类必须公开可见（包括公开嵌套类）、非抽象且不含未绑定泛型参数，按 IsAssignableFrom 判断。只在 ReflectionTypeLoadException 时保留成功加载类型，其他错误正常传播；需要诊断缺失依赖时直接调用 Assembly.GetTypes 查看 LoaderExceptions。
- 这些扩展使用运行时反射，不承诺 NativeAOT / 裁剪兼容，也未添加全局类型缓存以免阻碍可卸载程序集释放。

回归验证：`dotnet test M.Simple.Expand.Test/M.Simple.Expand.Test.csproj -c Release -p:GeneratePackageOnBuild=false --filter FullyQualifiedName~Reflection`。
