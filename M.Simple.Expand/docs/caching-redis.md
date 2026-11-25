# M.Simple.Caching.Redis 接入与使用

自动依赖缓存核心包，使用 StackExchange.Redis，支持 Redis 3.2+。在 ConnectionStrings:Redis 配置连接串，例如 127.0.0.1:6379,defaultDatabase=0；密码及 TLS 配置由宿主管理。

当前项目版本 `1.0.0.6`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Caching.Redis --version 1.0.0.6
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## appsettings.json 配置

在 API 项目的 appsettings.json 中合并以下完整配置。数值时间分别按分钟、秒读取：

```json
{
  "SimpleCache": {
    "Redis": {
      "ConnectionString": "127.0.0.1:6379",
      "Database": 4,
      "KeyPrefix": "MyApi:",
      "DefaultExpirationMinutes": 2,
      "CacheNullValues": false,
      "NullExpirationSeconds": 30,
      "FactoryTimeoutSeconds": 30,
      "MaxPayloadBytes": 1048576,
      "SerializerOptions": {
        "PropertyNameCaseInsensitive": true,
        "WriteIndented": false
      }
    }
  }
}
```

地址、端口和数据库编号按实际环境替换。需要密码时连接串可写为 `127.0.0.1:6379,password=your_password`；使用 TLS 时按服务要求增加 `ssl=true` 等选项。真实凭据可使用 User Secrets 或环境变量 `SimpleCache__Redis__ConnectionString` 覆盖。

此示例统一使用 SimpleCache:Redis 配置节，由下面的代码显式读取，不是包自动绑定。原先 ConnectionStrings:Redis 的方式仍可用，只需将连接串读取改为 GetConnectionString("Redis")，两种方式选择一种即可。

## Program.cs 配置

```csharp
using M.Simple.Caching.Redis; // 本次新增：Redis 缓存注册扩展。
using System.Text.Json; // 本次新增：缓存 JSON 序列化配置。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 本次新增：读取 Redis 配置节，只注册一个缓存后端。
var redis = builder.Configuration.GetSection("SimpleCache:Redis");
builder.Services.AddSimpleRedisCache(options =>
{
    options.ConnectionString = redis["ConnectionString"]
        ?? throw new InvalidOperationException("SimpleCache:Redis:ConnectionString is not configured.");
    options.Database = redis.GetValue<int>("Database", -1);
    options.KeyPrefix = $"{redis["KeyPrefix"] ?? "MyApi:"}{builder.Environment.EnvironmentName}:";
    options.DefaultExpiration = TimeSpan.FromMinutes(redis.GetValue<double>("DefaultExpirationMinutes", 10));
    options.CacheNullValues = redis.GetValue<bool>("CacheNullValues", false);
    options.NullExpiration = TimeSpan.FromSeconds(redis.GetValue<double>("NullExpirationSeconds", 30));
    options.FactoryTimeout = TimeSpan.FromSeconds(redis.GetValue<double>("FactoryTimeoutSeconds", 30));
    options.MaxPayloadBytes = redis.GetValue<int>("MaxPayloadBytes", 1024 * 1024);
    options.SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = redis.GetValue<bool>("SerializerOptions:PropertyNameCaseInsensitive", true),
        WriteIndented = redis.GetValue<bool>("SerializerOptions:WriteIndented", false)
    };
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 完整注册参数与默认值

以下包含 SimpleRedisCacheOptions 自身和继承自 SimpleCacheOptions 的全部注册参数。上方 JSON 中数据库 4、默认过期 2 分钟是示例值，非包的默认值。

| 参数 | 包默认值 | 作用 |
| --- | --- | --- |
| ConnectionString | 空，必须配置 | StackExchange.Redis 连接串，支持密码、TLS 等客户端选项 |
| Database | -1 | 使用连接串指定的默认数据库；非负值显式选择数据库 |
| KeyPrefix | 空字符串 | 应用隔离前缀，最长 512 字符；示例另追加运行环境，默认工作台管理模式要求非空 |
| DefaultExpiration | 10 分钟 | 未提供单条过期策略时的默认过期时间 |
| CacheNullValues | false | GetOrCreateAsync 是否自动缓存工厂返回的 null |
| NullExpiration | 30 秒 | null 缓存项的绝对有效期上限 |
| FactoryTimeout | 30 秒 | 共享加载工厂超时，范围 1 毫秒至 1 天 |
| MaxPayloadBytes | 1048576（1 MiB） | 单次写入 JSON 的字节数上限，必须大于 0 |
| SerializerOptions | JsonSerializerDefaults.Web | System.Text.Json 选项；上方展示其中常用的大小写匹配和格式化设置 |

DefaultExpiration、NullExpiration 的有效范围为 1 毫秒至 3650 天。SerializerOptions 还可设置转换器等标准 JSON 选项，代码与读写两端应保持兼容；它不止包含示例中的两个属性。

CacheNullValues=false 不会禁止显式 SetAsync 存储 null。GetOrCreateAsync 的单条策略也可覆盖是否缓存 null；所有 null 项仍受 NullExpiration 上限约束。单条绝对/滑动过期和 NeverExpires 使用 SimpleCacheEntryOptions，不属于注册参数，详见[缓存核心用法](caching.md)。MaxEntryCount、MaxSizeBytes 是内存后端参数，不适用于 Redis。

## 使用说明

只调用 AddSimpleRedisCache，不额外调用 AddSimpleMemoryCache 或 AddSimpleMongoCache。首次使用建立并复用连接，随容器释放。缓存项以 Hash 存储；并发加载合并仅限当前进程，不提供分布式加载锁。取消等待不保证已发送命令未执行。

## 实际使用示例

注册 Redis 后端后，业务类注入核心接口 `ISimpleCache`，无需自行创建 Redis 连接。以下为普通业务服务，不增加 HTTP 接口。

### 构造函数注入与缓存读写

```csharp
using M.Simple.Caching;

public sealed record ProductDto(long Id, string Name, decimal Price);

public sealed class ProductCacheService(ISimpleCache cache)
{
    private static string Key(long id) => $"product:{id}";

    public Task SetAsync(ProductDto product, CancellationToken token = default)
        => cache.SetAsync(Key(product.Id), product,
            new SimpleCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, token);

    public Task<CacheResult<ProductDto>> GetAsync(long id, CancellationToken token = default)
        => cache.TryGetAsync<ProductDto>(Key(id), token);

    public Task<bool> ExistsAsync(long id, CancellationToken token = default)
        => cache.ExistsAsync(Key(id), token);

    public Task RemoveAsync(long id, CancellationToken token = default)
        => cache.RemoveAsync(Key(id), token);
}
```

在 Program.cs 的 Build() 之前注册业务服务；已有程序集扫描覆盖该类型时按项目实际注册方式处理：

```csharp
builder.Services.AddScoped<ProductCacheService>();
```

在现有业务类中通过构造函数注入 ProductCacheService，下面 productCache 为已注入实例，token 为当前取消令牌：

```csharp
await productCache.SetAsync(new ProductDto(1, "商品 A", 19.90m), token);

var result = await productCache.GetAsync(1, token);
if (result.Found)
{
    ProductDto? product = result.Value;
    // 使用命中的数据；Found 用于区分未命中和有效的 null、0、false。
}

bool exists = await productCache.ExistsAsync(1, token);
await productCache.RemoveAsync(1, token);
```

业务只传相对 Key，如 product:1，不要重复拼接 KeyPrefix。若配置前缀为 MyApi:Development:，实际 Redis Key 为 MyApi:Development:product:1。写入和读取必须使用相同泛型类型；例如写 ProductDto，就读取 ProductDto，而非 object 或其他结构相似的类型。

### 未命中时加载数据库并自动缓存

以下接口由项目实际仓储实现并注册。工厂仅在缓存未命中时执行，数据库返回的结果由缓存自动保存：

```csharp
using M.Simple.Caching;

public interface IProductReader
{
    Task<ProductDto?> FindAsync(long id, CancellationToken token);
}

public sealed class ProductQueryService(ISimpleCache cache, IProductReader products)
{
    public Task<ProductDto?> GetAsync(long id, CancellationToken token = default)
        => cache.GetOrCreateAsync<ProductDto?>(
            $"product:{id}",
            ct => products.FindAsync(id, ct),
            new SimpleCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                CacheNullValues = true
            },
            token);
}
```

注册 ProductQueryService 和项目的 IProductReader 实现后，在现有业务流程中调用 GetAsync 即可。不存在的商品可短时缓存 null，实际有效期仍受全局 NullExpiration 上限约束（默认 30 秒）。不需要缓存 null 时将 CacheNullValues 设为 false。

GetOrCreateAsync 在同一缓存实例、同一 Key 的并发未命中时合并加载；不同应用实例仍可能分别访问数据库。工厂应使用传入的 ct，单个调用方取消等待不会取消其他调用方共享的加载。

### 更新数据库后删除缓存

先完成数据库保存或事务提交，再删除缓存，让下次查询重新加载：

```csharp
await unitOfWork.SaveChangesAsync(token);
await cache.RemoveAsync($"product:{product.Id}", token);
```

unitOfWork、cache 和 product 为现有业务对象；如开启了显式事务，应在提交成功后删除。缓存不参与数据库事务，缓存删除失败会向调用方抛出，业务需按一致性要求处理；不要在提交前删除后认为数据库与缓存已保持一致。

### 单条过期策略

以下写法分别展示不同策略，按业务选择：

```csharp
// 滑动两分钟，且总有效期不超过三十分钟。
await cache.SetAsync("product:hot", product, new SimpleCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(2),
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
}, token);

// 不主动过期；不可同时设置绝对或滑动期限。
await cache.SetAsync("product:catalog", product, new SimpleCacheEntryOptions
{
    NeverExpires = true
}, token);
```

不传 SimpleCacheEntryOptions 时采用全局 DefaultExpiration。ExistsAsync 不刷新滑动期限；TryGetAsync 和 GetOrCreateAsync 命中会刷新。缓存是 JSON 快照，修改返回对象不会自动更新 Redis，需要再次 SetAsync。

### 读取已有 Redis 原生数据

ISimpleCache 用于本包写入的缓存格式。已有 Redis String、Hash、List 等数据应使用 `ISimpleNativeCache`，具体类型读取及完整 Key 范围规则见下方“独立业务原生读取”和[核心原生读取示例](caching.md#独立原生读取)。不要使用 ISimpleCache 直接读取其他系统写入的普通 Redis String。

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
