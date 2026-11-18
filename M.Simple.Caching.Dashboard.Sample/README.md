# 缓存管理 Web 示例

本示例仅允许 Development，并绑定 http://127.0.0.1:5189。演示登录不校验密码，仅用于本机验证；业务项目必须使用自己的用户认证和管理员权限策略。

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project M.Simple.Caching.Dashboard.Sample -- --backend memory
```

访问 http://127.0.0.1:5189/cache/，跳转开发示例登录后点击“以演示管理员进入”。应用初始化 12 条商品和滑动过期、空值、工厂加载示例，可查看详情、查询 product:、勾选批量删除和预览前缀清理。

Redis：

```powershell
$env:M_SIMPLE_REDIS = "127.0.0.1:6379"
dotnet run --project M.Simple.Caching.Dashboard.Sample -- --backend redis
```

MongoDB：

```powershell
$env:M_SIMPLE_MONGODB = "mongodb://127.0.0.1:27017/?directConnection=true"
dotnet run --project M.Simple.Caching.Dashboard.Sample -- --backend mongo
```

只读模式追加 --readonly true。所有模式使用 CacheDashboardSample: 前缀；MongoDB 数据库为 M_Simple_Cache_Dashboard_Sample。不要将示例连接到需要保留相同前缀数据的环境，启动会写入示例 Key；程序不清空其他数据。

详细接入说明见 [Dashboard README](../M.Simple.Caching.Dashboard/README.md)。

开发示例使用进程临时 Data Protection 密钥，重启后需重新登录。正式 Dashboard 包不会替换宿主密钥配置。
