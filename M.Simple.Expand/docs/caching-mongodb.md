# M.Simple.Caching.MongoDB 接入与使用

自动依赖缓存核心包，面向 MongoDB 6.0+。在 ConnectionStrings:MongoDB 配置连接串，例如 mongodb://127.0.0.1:27017/；使用专用缓存集合。

当前项目版本 `1.0.0.5`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Caching.MongoDB --version 1.0.0.5
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

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
