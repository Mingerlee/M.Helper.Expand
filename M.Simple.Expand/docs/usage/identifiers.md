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

- GenerateTimeStampGuidCode 保留 UTC 自 0001-01-01 起的毫秒数前缀，后接完整 32 位大写 GUID。后缀由旧实现的 8 位增加至 32 位，返回值增加 24 个字符；调用方需核对数据库字段及长度校验。它不是 Unix 时间戳；同一毫秒内不保证生成顺序，唯一性依赖完整 GUID。

- SnowflakeIdGenerator(workerId) 应长期复用，同一分布式系统为不同生成节点配置不同 workerId；NextId、NextIdString 返回数值和文本形式。

- ShortCodeGenerator.GenerateShortCode 复用固定 workerId=1 的线程安全生成器，输出仍为 11 位 Base62 短码。已验证同一进程内连续、并发各生成 1 万个无重复；不保证不同进程、不同机器或进程重启后的唯一性。分布式场景应使用自行分配不同节点号的 SnowflakeIdGenerator，避免多个节点共享同一 workerId。

- SnowCodeGenerator、UniqueCodeGenerator 属于不同历史编码方案，不应混用唯一性约定。示例验证基础调用，回归测试补充了时间戳 GUID 和短码的连续、并发生成；未执行多机器、时钟回拨或进程重启验证。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
