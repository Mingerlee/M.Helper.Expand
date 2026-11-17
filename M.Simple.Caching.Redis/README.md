# M.Simple.Caching.Redis

.NET 8 Redis 缓存包，当前源码版本 **1.0.0.1**。自动依赖同版本核心包，使用 StackExchange.Redis 2.11.3；支持 Redis 5+ 的原子 Lua 操作。

```powershell
dotnet add package M.Simple.Caching.Redis --version 1.0.0.1
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
