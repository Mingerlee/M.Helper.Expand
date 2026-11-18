# M.Simple.Caching.Redis

.NET 8 Redis 缓存包，当前源码版本 **1.0.0.4**。自动依赖核心包 1.0.0.3，使用 StackExchange.Redis 2.11.3；支持 Redis 3.2+ 的原子 Lua 操作。

```powershell
dotnet add package M.Simple.Caching.Redis --version 1.0.0.4
```

```csharp
using M.Simple.Caching;
using M.Simple.Caching.Redis;

builder.Services.AddSimpleRedisCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Redis")!;
    options.Database = -1; // 使用连接字符串的默认数据库。
    options.KeyPrefix = "ExampleApi:Production:";
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
});
```

只需此一次注册，业务注入 ISimpleCache。连接串可以包含密码、TLS 等官方客户端选项，由宿主管理配置。注册时不访问网络；首次使用建立并复用一个由包持有的 ConnectionMultiplexer，随容器释放。

每个缓存项存储为一个 Redis Hash。写入、过期检查、滑动刷新及绝对上限均由单 Key Lua 脚本原子执行；使用 Redis 服务端时间并设置原生 TTL。普通读取不会将已经过期的旧项重新写入；ExistsAsync 不刷新滑动期限。不使用全库清空或通配符删除。

取消令牌可以停止等待客户端操作，但已经发送的命令仍可能完成；该限制也适用于被取消的 Set/Remove。并发加载合并仅限当前进程，不提供跨实例锁。一个前缀只能由此包维护，不要混用其他数据结构。

其他配置、空值策略和缓存接口规则见[核心说明](../M.Simple.Caching/README.md)。

```powershell
dotnet pack M.Simple.Caching.Redis/M.Simple.Caching.Redis.csproj -c Release -o artifacts/nuget
```

安装命令要求包源中已有相应版本；本仓库不会自动发布到 NuGet。

## 可选管理页面

安装 [M.Simple.Caching.Dashboard](../M.Simple.Caching.Dashboard/README.md) 默认管理当前非空 KeyPrefix 内的缓存。管理页面支持 Scope 配置：CurrentApplication 保持默认范围；Prefixes 按配置的完整前缀筛选；All 显示当前后端存储范围内的所有有效缓存。后两种模式的页面与管理 API 使用完整 Key，业务缓存的 KeyPrefix 不变；配置示例见 Dashboard README。内存、Redis、MongoDB 行为一致，查询不续期，所有操作要求宿主认证授权。配置与业务缓存读写独立；使用时请同步升级核心包 1.0.0.3、Dashboard 1.0.0.2，以及所选后端适配包。

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

仅使用 Redis 3.2 兼容修复时，1.0.0.3 起即可；使用本次管理范围配置时，请升级 Redis 至 1.0.0.4、核心至 1.0.0.3、Dashboard 至 1.0.0.2。管理删除同时验证完整缓存字段，避免删除字段不完整的普通 Hash。
