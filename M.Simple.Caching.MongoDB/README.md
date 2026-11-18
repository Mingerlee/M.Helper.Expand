# M.Simple.Caching.MongoDB

.NET 8 MongoDB 缓存适配包，当前源码版本 **1.0.0.2**。自动依赖同版本核心包，使用 MongoDB.Driver 3.9.0；面向 MongoDB 6.0+。

```powershell
dotnet add package M.Simple.Caching.MongoDB --version 1.0.0.2
```

```csharp
using M.Simple.Caching;
using M.Simple.Caching.MongoDB;

builder.Services.AddSimpleMongoCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDB")!;
    options.DatabaseName = "ExampleApi";
    options.CollectionName = "CacheEntries";
    options.KeyPrefix = "ExampleApi:Production:";
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
});
```

只需一次注册，业务注入 ISimpleCache。注册时解析配置但不访问网络；首次操作创建 TTL 索引，需具有目标缓存集合的索引创建及读写权限。索引初始化失败不标记为成功，下次操作可以重试。

缓存文档包括 _id（带前缀 Key）、typeName、JSON UTF-8 二进制 payload、可选 absoluteExpiresAt、slidingMilliseconds 和 expiresAt；时间由 MongoDB 的 $$NOW 计算。不主动过期的项目省略日期字段。

- 在 expiresAt 上建立 expireAfterSeconds=0 的 TTL 索引，负责后台物理清理。
- TTL 清理存在延迟；所有读与存在性检查都会独立过滤逻辑过期项，因此过期文档尚未删除也不会命中。
- 写入使用单文档原子 Upsert；遇到并发首次写入的重复键冲突时有限重试。
- 滑动命中会额外执行条件更新，并在服务端计算新期限及绝对上限。只刷新当前仍有效的文档，不重新插入已删除项，也不覆盖并发更新的 payload。
- ExistsAsync 只检查存在性，不刷新期限。滑动过期会增加数据库读写，按需启用。
- 集合为缓存专用；包不会删除集合、覆盖现有不兼容索引或清理其他应用的数据。

连接/序列化错误直接传播。取消不保证已提交的服务端修改回滚。缓存不是正式业务数据存储，也不提供跨实例分布式锁。其他统一行为见[核心说明](../M.Simple.Caching/README.md)。

```powershell
dotnet pack M.Simple.Caching.MongoDB/M.Simple.Caching.MongoDB.csproj -c Release -o artifacts/nuget
```

安装要求包源存在相应版本；本仓库只本地构建，不自动推送 NuGet。

## 可选管理页面

安装 [M.Simple.Caching.Dashboard](../M.Simple.Caching.Dashboard/README.md) 可管理当前 KeyPrefix 内的缓存。请设置专属非空前缀，并使用带 ISimpleCacheAdminStore 管理接口的配套源码/包版本。管理查询不续期，默认要求宿主认证授权；业务缓存调用方式不变。
