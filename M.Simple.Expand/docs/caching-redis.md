# M.Simple.Caching.Redis 接入与使用

自动依赖缓存核心包，使用 StackExchange.Redis，支持 Redis 3.2+。在 ConnectionStrings:Redis 配置连接串，例如 127.0.0.1:6379,defaultDatabase=0；密码及 TLS 配置由宿主管理。

当前项目版本 `1.0.0.6`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Caching.Redis --version 1.0.0.6
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## Program.cs 配置

```csharp
using M.Simple.Caching.Redis; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：注册 Redis 后端，自动提供 ISimpleCache。
builder.Services.AddSimpleRedisCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");
    options.KeyPrefix = "MyApi:Production:";
    options.Database = -1;
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

只调用 AddSimpleRedisCache，不额外调用 AddSimpleMemoryCache 或 AddSimpleMongoCache。首次使用建立并复用连接，随容器释放。缓存项以 Hash 存储；并发加载合并仅限当前进程，不提供分布式加载锁。取消等待不保证已发送命令未执行。

## Redis 3.2 兼容

从 1.0.0.3 起支持 Redis 3.2：

- 多字段写入使用 HMSET，保持原有 Hash 字段和 JSON 格式，已有缓存无需迁移。
- 使用服务端 TIME 后可能写入的脚本启用 redis.replicate_commands()，兼容 3.2 的命令效果复制要求；滑动过期和绝对上限规则不变。
- 管理删除优先使用 UNLINK。服务器不认识该命令时使用 DEL；权限和其他服务端错误仍向调用方传播。Redis 3.2 的 DEL 为同步删除，单条 JSON 大小继续受 MaxPayloadBytes 限制。
- 普通删除、列表 SCAN 和不续期详情读取均保持原接口。客户端配置 Database 可显式选择 db11 等逻辑数据库。

兼容性集成测试使用独立随机前缀，仅清理本轮创建的 Key，不清空数据库：

```powershell
$env:M_SIMPLE_TEST_REDIS = "127.0.0.1:6379,defaultDatabase=11"
dotnet test M.Simple.Caching.Test/M.Simple.Caching.Test.csproj -c Release --filter TestCategory=RedisCompatibility
```

仅使用 Redis 3.2 兼容修复时，1.0.0.3 起即可；使用本次管理范围配置时，请升级 Redis 至 1.0.0.6、核心至 1.0.0.5、Dashboard 至 1.0.0.3。CurrentApplication、AllManaged 的管理删除验证完整缓存字段；All、Prefixes 则允许删除范围内的原生数据。


## 独立业务原生读取

新增 ISimpleNativeCache，与管理页面的范围无关，使用完整 Key；默认只允许业务 KeyPrefix，可通过 AddSimpleNativeCache 独立选择 NativeCacheScope.Prefixes 或 All。TryGetAsync<T> 读取 Redis String：byte[] 原样返回、string 严格 UTF-8 解码、其他 T 按 JSON 映射。Hash/List/Set/SortedSet 分别使用 HashGetAllAsync、ListRangeAsync、SetMembersAsync、SortedSetRangeAsync；不自动解包本包的 Hash。

只读脚本兼容 Redis 3.2，不修改 TTL。返回数据受字节数、集合项数上限约束，超限或类型错误明确报错，不返回管理页面那样的截断预览。配置、协议与可运行示例见[核心说明](caching.md#独立原生读取)。


## 接入注意事项

只调用 AddSimpleRedisCache，不额外调用 AddSimpleMemoryCache 或 AddSimpleMongoCache。首次使用建立并复用连接，随容器释放。缓存项以 Hash 存储；并发加载合并仅限当前进程，不提供分布式加载锁。取消等待不保证已发送命令未执行。
