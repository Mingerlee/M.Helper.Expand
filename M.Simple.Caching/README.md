# M.Simple.Caching

.NET 8 缓存核心包，当前源码版本 **1.0.0.1**。包含统一接口、JSON 快照、独立内存缓存、过期策略及同一进程内的异步加载合并。可用于 ASP.NET Core、Worker 和控制台，不依赖 ASP.NET Core 共享框架。

## 安装与注册

```powershell
dotnet add package M.Simple.Caching --version 1.0.0.1
```

```csharp
using M.Simple.Caching;

builder.Services.AddSimpleMemoryCache(options =>
{
    options.KeyPrefix = "ExampleApi:Development:";
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.MaxEntryCount = 10_000;
    options.MaxSizeBytes = 64L * 1024 * 1024;
});
```

每个容器只注册一个实现。Redis/MongoDB 包自动依赖核心包，调用对应注册方法即可，不要额外调用 AddSimpleMemoryCache。重复注册会抛出异常，避免实现被意外覆盖。

安装要求版本已发布到所用源；仓库只本地构建，不自动推送 NuGet。

## 使用

```csharp
public sealed class ProductService(ISimpleCache cache)
{
    public Task<ProductDto?> GetAsync(long id, CancellationToken cancellationToken)
        => cache.GetOrCreateAsync<ProductDto?>(
            $"product:{id}",
            ct => LoadFromDatabaseAsync(id, ct),
            new SimpleCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            cancellationToken);
}
```

上面的 LoadFromDatabaseAsync 由业务实现。其他操作：

```csharp
await cache.SetAsync("product:1", product);
var result = await cache.TryGetAsync<ProductDto>("product:1");
if (result.Found)
{
    var value = result.Value; // Found 与 null、0、false 的值分开判断。
}
bool exists = await cache.ExistsAsync("product:1");
await cache.RemoveAsync("product:1");
```

数据库更新成功后再删除相应缓存。缓存不参与数据库事务。

## 过期与空值

- 未设置单条策略时使用 DefaultExpiration，默认 10 分钟。
- 只设置 SlidingExpiration 表示纯滑动过期；同时设置绝对和滑动期限时，读取刷新滑动期限，但不超过绝对上限。
- NeverExpires=true 表示不主动过期，不能同时配置绝对/滑动期限；仍可能因内存容量被淘汰。
- ExistsAsync 不刷新滑动期限；TryGetAsync 和 GetOrCreateAsync 命中会刷新。
- 支持的相对期限为 1 毫秒至 3650 天；远端后端按毫秒向上取整。
- GetOrCreateAsync 默认不缓存 null。可全局设置 CacheNullValues=true，或在单条策略覆盖。
- SetAsync 显式存储 null 时视为有效缓存项。所有 null 项均受 NullExpiration（默认 30 秒）的绝对上限约束，NeverExpires 也不会使 null 永久缓存。

```csharp
var item = await cache.GetOrCreateAsync<ProductDto?>(
    "product:missing",
    ct => FindProductAsync(ct),
    new() { CacheNullValues = true });

await cache.SetAsync("hot", product, new()
{
    SlidingExpiration = TimeSpan.FromMinutes(2),
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
});
```

## 并发、取消与更新

- 同一个 ISimpleCache 单例、同一个 Key 的并发未命中共享一次加载。不同泛型类型不能共享 Key；第一个加载者的工厂与策略用于此次共享加载。
- 工厂抛出的异常不会缓存，下一次可以重试。
- 调用方 CancellationToken 只取消自己的等待，不取消其他调用方共享的工厂。
- 工厂使用独立的 FactoryTimeout（默认 30 秒，最多 1 天）及容器关闭令牌；超时抛出 OperationCanceledException。忽略令牌的业务代码仍可能在后台执行，但晚到结果不会写入缓存。
- 本进程内显式 Set/Remove 会阻止此前工厂完成后覆盖/重新写入缓存。已经等待旧工厂的调用者仍可能收到旧返回值。
- Redis/MongoDB 共享存储，但不提供分布式加载锁；不同实例可能分别加载。跨实例的数据库更新与缓存失效也不具有原子性，需业务按一致性要求协调。

## 序列化、容量与生命周期

所有实现统一使用 System.Text.Json，存入/取出均为快照，不共享调用方的可变对象引用。缓存时与读取时应使用相同泛型类型；类型标记不包含 DTO 所在程序集的版本号，但升级时仍需保持 DTO JSON 结构兼容，必要时更新 KeyPrefix。SerializerOptions 可在注册时设置。

MaxPayloadBytes 默认 1 MiB，限制单次写入的 JSON 大小。内存缓存使用独立 MemoryCache，不修改宿主已有 IMemoryCache 的设置；同时限制条目数与估算字节权重（JSON、Key、类型名及固定开销），实际 GC 内存不是硬配额。容量紧张时可能淘汰或拒绝缓存项；业务应始终能够重新加载。

KeyPrefix 默认空字符串，推荐包含应用和环境。原始 Key 不能空白，前缀和 Key 总长不能超过 1024 字符。注册后不要修改配置对象。ISimpleCache 和后端均为单例，由 DI 容器释放；工厂不要捕获生命周期短于缓存服务的资源，需使用时自行创建作用域。

后端连接/序列化错误会传播给调用方，不会静默降级成内存或未命中。取消网络等待不能保证服务端操作回滚。

## 构建、测试与示例

```powershell
dotnet test M.Simple.Caching.Test/M.Simple.Caching.Test.csproj -c Release
dotnet run --project M.Simple.Caching.Sample -- memory
dotnet pack M.Simple.Caching/M.Simple.Caching.csproj -c Release -o artifacts/nuget
```

Redis/MongoDB 测试为真实服务测试；未配置下面两个环境变量时，相应测试报告为未执行（Inconclusive），不会伪造为内存后端。测试使用独立随机前缀和 MongoDB 数据库，结束后仅清理自己的数据。

```powershell
$env:M_SIMPLE_TEST_REDIS = "127.0.0.1:6379"
$env:M_SIMPLE_TEST_MONGODB = "mongodb://127.0.0.1:27017/?directConnection=true"
dotnet test M.Simple.Caching.Test/M.Simple.Caching.Test.csproj -c Release
```

- [Redis 适配](../M.Simple.Caching.Redis/README.md)
- [MongoDB 适配](../M.Simple.Caching.MongoDB/README.md)
- [可运行示例](../M.Simple.Caching.Sample/README.md)
