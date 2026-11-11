# GUID 与编码

GUID 格式、雪花 ID 与短码。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- identifiers
```

```csharp
using M.Simple.Expand;

static partial class Demo
{
    public static void Identifiers()
    {
        var guid = Guid.NewGuid();
        Check(guid.GetNoHyphenString().Length == 32, "无连接符 GUID");
        // 同一进程复用生成器；不同节点需分配不同 workerId。
        var generator = new SnowflakeIdGenerator(workerId: 1);
        var first = generator.NextId();
        var second = generator.NextId();
        Check(first != second, "雪花 ID 连续生成");
        Console.WriteLine("GUID 格式与复用雪花生成器验证通过（ID 不作为固定输出展示）");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- GetNoHyphenString 返回无连接符 GUID；GetNextGuid、GetEncryptSecret 按现有实现提供 GUID 派生工具。

- SnowflakeIdGenerator(workerId) 应长期复用，同一分布式系统为不同生成节点配置不同 workerId；NextId、NextIdString 返回数值和文本形式。

- SnowCodeGenerator、UniqueCodeGenerator、ShortCodeGenerator 属于不同历史编码方案，不能仅凭名称认定具有全局唯一性。ShortCodeGenerator 每次新建固定 worker 的雪花生成器，未经并发唯一性验证。

- 示例仅验证单实例连续生成，未执行多机器、时钟回拨、进程重启或高并发唯一性测试。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
