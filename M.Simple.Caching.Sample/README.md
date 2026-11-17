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
