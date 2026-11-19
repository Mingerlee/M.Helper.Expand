# M.Simple.Caching 可运行示例

要求 .NET 8 SDK。在仓库根目录执行：

```powershell
dotnet run --project M.Simple.Caching.Sample -- memory
```

输出两次相同商品结果、加载次数、null 命中与删除结果。示例同时引用三个项目方便切换；实际业务只安装所用后端包，注册一次即可。

Redis：

```powershell
$env:M_SIMPLE_REDIS = "127.0.0.1:6379"
dotnet run --project M.Simple.Caching.Sample -- redis
```

MongoDB：

```powershell
$env:M_SIMPLE_MONGODB = "mongodb://127.0.0.1:27017/?directConnection=true"
$env:M_SIMPLE_CACHE_DATABASE = "M_Simple_Cache_Sample"
dotnet run --project M.Simple.Caching.Sample -- mongo
```

示例只写入 M.Simple.Caching.Sample: 前缀的两个 Key，完成后删除自己的 Key；MongoDB 首次操作会创建缓存集合及 TTL 索引。连接配置由环境变量提供，不写入仓库。业务使用方式和并发规则见[核心说明](../M.Simple.Caching/README.md)。

## 独立原生读取示例

使用已有的连接环境变量，命令末尾增加 `native`：

```powershell
dotnet run --project M.Simple.Caching.Sample -- memory native
dotnet run --project M.Simple.Caching.Sample -- redis native
dotnet run --project M.Simple.Caching.Sample -- mongo native
```

示例使用 `ISimpleNativeCache` 和完整 Key 读取。Redis 直接写入普通 String/Hash/List，展示 JSON DTO 映射与集合读取；MongoDB 直接插入普通文档并按 BSON 规则映射；内存读取当前独立实例的 JSON payload。原生接口本身没有写入或删除方法，示例使用后端客户端准备数据，并在 finally 中只清理本次随机 Key/文档。

原生范围默认 CurrentApplication，独立于 Dashboard 的 Scope；不会更改 `ISimpleCache` 的业务行为。详细配置和读取限制见[核心说明](../M.Simple.Caching/README.md)。当前只修改代码，不调整版本号或重新打包。
