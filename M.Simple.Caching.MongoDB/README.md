# M.Simple.Caching.MongoDB

.NET 8 MongoDB 缓存适配包，当前源码版本 **1.0.0.5**。自动依赖同版本核心包，使用 MongoDB.Driver 3.9.0；面向 MongoDB 6.0+。

```powershell
dotnet add package M.Simple.Caching.MongoDB --version 1.0.0.5
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

安装 [M.Simple.Caching.Dashboard](../M.Simple.Caching.Dashboard/README.md) 默认管理当前非空 KeyPrefix 内的缓存。管理页面支持 Scope 配置：CurrentApplication 保持默认范围；Prefixes 在全部数据中按配置的完整前缀筛选；All 显示当前后端存储范围内的全部数据（包含非本包数据）；AllManaged 只显示所有应用通过本包存入的有效缓存。除 CurrentApplication 外，页面与管理 API 使用完整 Key，业务缓存的 KeyPrefix 不变；配置示例见 Dashboard README。内存、Redis、MongoDB 行为一致，查询不续期，所有操作要求宿主认证授权。配置与业务缓存读写独立；使用时请同步升级核心包 1.0.0.5、Dashboard 1.0.0.3，以及所选后端适配包。

## 独立业务原生读取

新增 ISimpleNativeCache，按完整 Key 精确匹配字符串 _id，使用 BSON 驱动映射整文档为 T；可返回 BsonDocument 或原始 BSON byte[]，不会自动读取本包 payload，也不会猜测 ObjectId。不创建 TTL 索引，不刷新有效期，不把业务 expiresAt 字段视为逻辑过期；服务器尚未物理清理的文档仍可读。

读取范围通过 AddSimpleNativeCache 独立配置，默认 CurrentApplication，不受 Dashboard.Scope 扩大影响；超过 MaxPayloadBytes 时通过服务器投影限制正文传输并报错。JSON/BSON 映射规则、范围与可运行示例见[核心说明](../M.Simple.Caching/README.md#独立原生读取)。
