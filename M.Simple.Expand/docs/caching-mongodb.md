# M.Simple.Caching.MongoDB 接入与使用

自动依赖缓存核心包，面向 MongoDB 6.0+。在 ConnectionStrings:MongoDB 配置连接串，例如 mongodb://127.0.0.1:27017/；使用专用缓存集合。

当前项目版本 `1.0.0.5`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Caching.MongoDB --version 1.0.0.5
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## appsettings.json 配置

在 API 项目的 `appsettings.json` 中合并以下示例，替换为实际环境值：

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://127.0.0.1:27017/"
  }
}
```

对应 GetConnectionString("MongoDB") 读取的 ConnectionStrings:MongoDB。数据库名和集合名由下方 DatabaseName、CollectionName 指定；如需认证或 TLS，在连接串中配置相应选项。

真实凭据可通过 User Secrets 或环境变量覆盖，不必写入配置文件；环境变量将键中的冒号替换为双下划线，例如 `ConnectionStrings__MongoDB`。

## Program.cs 配置

```csharp
using M.Simple.Caching.MongoDB; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：注册 MongoDB 缓存后端，自动提供 ISimpleCache。
builder.Services.AddSimpleMongoCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDB")
        ?? throw new InvalidOperationException("ConnectionStrings:MongoDB is not configured.");
    options.DatabaseName = "MyApiCache";
    options.CollectionName = "CacheEntries";
    options.KeyPrefix = "MyApi:Production:";
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

只注册一个后端。首次缓存操作创建 TTL 索引，账号需具备目标集合的索引创建和读写权限。TTL 后台清理有延迟，但缓存接口会过滤逻辑过期项。原生文档读取与缓存 payload 读取是不同接口。

## 独立业务原生读取

新增 ISimpleNativeCache，按完整 Key 精确匹配字符串 _id，使用 BSON 驱动映射整文档为 T；可返回 BsonDocument 或原始 BSON byte[]，不会自动读取本包 payload，也不会猜测 ObjectId。不创建 TTL 索引，不刷新有效期，不把业务 expiresAt 字段视为逻辑过期；服务器尚未物理清理的文档仍可读。

读取范围通过 AddSimpleNativeCache 独立配置，默认 CurrentApplication，不受 Dashboard.Scope 扩大影响；超过 MaxPayloadBytes 时通过服务器投影限制正文传输并报错。JSON/BSON 映射规则、范围与可运行示例见[核心说明](caching.md#独立原生读取)。


## 接入注意事项

只注册一个后端。首次缓存操作创建 TTL 索引，账号需具备目标集合的索引创建和读写权限。TTL 后台清理有延迟，但缓存接口会过滤逻辑过期项。原生文档读取与缓存 payload 读取是不同接口。

## 业务缓存使用示例

MongoDB 后端仍通过 ISimpleCache 读写，无需在业务类中直接调用 MongoDB 驱动：

```csharp
using M.Simple.Caching;

public sealed record CustomerDto(long Id, string Name);

public sealed class CustomerCacheService(ISimpleCache cache)
{
    public Task SaveAsync(CustomerDto customer, CancellationToken token = default)
        => cache.SetAsync($"customer:{customer.Id}", customer,
            new SimpleCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, token);

    public Task<CacheResult<CustomerDto>> FindAsync(long id, CancellationToken token = default)
        => cache.TryGetAsync<CustomerDto>($"customer:{id}", token);

    public Task RemoveAsync(long id, CancellationToken token = default)
        => cache.RemoveAsync($"customer:{id}", token);
}
```

Build() 前注册 AddScoped<CustomerCacheService>()，在现有业务类中注入后调用：

```csharp
await customers.SaveAsync(new CustomerDto(1, "客户 A"), token);
var result = await customers.FindAsync(1, token);
if (result.Found)
{
    CustomerDto? customer = result.Value;
}
await customers.RemoveAsync(1, token);
```

customers 为已注入服务，token 为取消令牌。仅传相对 Key，前缀由包添加；读取泛型类型与写入保持一致。未命中加载、空值缓存和滑动过期用法与[缓存核心示例](caching.md)一致。数据库提交成功后再删除相关缓存，缓存不参与数据库事务。
