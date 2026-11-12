# M.Helper.Logging

面向 .NET 8 / ASP.NET Core 的轻量日志包：标准 `ILogger<T>`、本地 SQLite、应用内嵌 Dashboard。无需 Serilog、ORM、前端构建工具或外部日志服务。

## 快速接入

包发布后安装：`dotnet add package M.Helper.Logging`。仓库内可使用项目引用，当前尚未发布到 NuGet。

```csharp
using M.Helper.Logging;

var builder = WebApplication.CreateBuilder(args);
// 沿用宿主现有的认证方案和授权策略。
builder.Services.AddHelperLogging();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapHelperLogDashboard("/logs").RequireAuthorization("LogAdmin");
app.Run();
```

认证方案和 `LogAdmin` 策略由宿主注册。Dashboard 默认要求已登录；生产应用应明确限制管理员权限。注册本包不会清除现有日志提供程序。

宿主使用 `UsePathBase` 时，将其放在 `UseAuthentication` / `UseAuthorization` 之前，显式保留这些中间件顺序。

业务代码继续使用标准日志接口：

```csharp
logger.LogInformation("订单 {OrderId} 创建成功", orderId);
logger.LogError(exception, "订单 {OrderId} 保存失败", orderId);
using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = userId }))
{
    logger.LogWarning("订单 {OrderId} 需要人工确认", orderId);
}
```

自动保存当前 Activity 的 TraceId / SpanId；无 Activity 时为空。不主动采集请求体、响应体、Cookie、Token 或用户身份。

## 配置

```csharp
builder.Services.AddHelperLogging(options =>
{
    options.ApplicationName = "BAMS.Api";
    options.DatabasePath = "App_Data/logs.db";
    options.MinimumLevel = LogLevel.Information;
    options.RetentionDays = 15;
    options.QueueCapacity = 10_000;
    options.BatchSize = 200;
    options.FlushInterval = TimeSpan.FromSeconds(1);
    options.ShutdownTimeout = TimeSpan.FromSeconds(5);
});
```

- 相对路径基于 `AppContext.BaseDirectory`，目录自动创建；启动时检查数据库权限。
- 默认过滤 `Microsoft.AspNetCore` 分类低于 Warning 的框架请求日志，避免页面轮询产生大量自身日志。可通过后续 `builder.Logging.AddFilter<HelperLoggerProvider>(...)` 覆盖。
- 宿主 Logging 过滤配置仍生效；开启 Debug / Trace 时需要同时允许宿主过滤器和本包 MinimumLevel。
- 字段默认最多 32,768 字符；属性和 Scope 超限保存有效 JSON 截断标识。普通对象通过 `ToString()` 快照，不深度遍历对象。
- `Enrich` 回调可在入队前脱敏消息、异常、属性和 Scope；需线程安全，不得保留并异步修改日志对象。业务主动写入的敏感信息需自行处理。
- 选项在注册时配置，运行期间不要修改；每个宿主仅调用一次 `AddHelperLogging`。

## 页面与 API

访问 `/logs` 或 `/logs/`，支持宿主 PathBase。所有页面/API 共用路由组授权配置。

- 保留期总数、今日日志、今日错误（含 Critical）、最近一小时。
- 时间、级别、日志来源精确匹配、消息/异常/属性关键词、TraceId 筛选。
- 日志详情、异常堆栈、结构化属性、Scope、复制 JSON、TraceId 关联查询。
- 自动刷新默认关闭，每 10 秒刷新；仅在第一页、详情关闭时生效。
- 当前进程的采集、落库、丢弃、采集失败、写入失败和运行状态。

| Dashboard 下的相对路径 | 用途 |
| --- | --- |
| `GET /api/logs` | `from/to/level/minimumLevel/category/keyword/traceId/beforeId/pageSize` 筛选 |
| `GET /api/logs/{id}` | 单条详情 |
| `GET /api/statistics?utcOffsetMinutes=480` | 统计，默认 UTC 今日边界 |
| `GET /api/status` | 当前进程状态 |

时间统一存 UTC，范围为 `[from,to)`，建议传携带时区的 ISO 8601。页面今日边界按浏览器当前 UTC 偏移计算。列表按 Id 倒序游标分页，每页默认 50 条、最大 200 条；API 默认不限时间，页面默认最近 24 小时。关键词采用区分大小写的字面量匹配，SQL 参数绑定。

日志内容通过文本 DOM 展示，不当作 HTML 执行；响应禁止缓存，页面不依赖 CDN。

## 可靠性与边界

- 唯一直接 NuGet 依赖为 `Microsoft.Data.Sqlite`，包含传递的 SQLite 原生运行库。其他能力来自 .NET 8 / `Microsoft.AspNetCore.App` 共享框架，消费端需 ASP.NET Core 运行时。
- 单后台写入器、事务批量提交、WAL、`synchronous=NORMAL`，读连接独立。
- 有界队列满时丢弃新日志，不阻塞业务；Formatter / Enrich 失败不抛给业务，计入 CaptureFailures。
- 写入失败保留批次，每秒重试；队列仍有容量上限。状态只暴露错误类型，内部错误不递归写日志。
- 正常停机在默认 5 秒内尽量排空；强制退出、超时、掉电时可能丢失日志，不适用于可靠审计账本。
- 每小时及启动时清理过期记录，每批 1,000 条。删除后空页可复用，但数据库文件不自动缩小；首版无磁盘配额、自动 VACUUM 或磁盘缓冲。
- 首版统计扫描保留期数据，关键词扫描筛选范围。建议限制保留期和查询范围；尚未做大规模性能基准。
- 每实例独立本地 SQLite 文件，不放在网络共享目录；容器需挂载持久化目录。首版不做多实例集中查询。
- 备份使用 SQLite 在线备份机制，或停机后备份；不能运行中只复制 `.db` 而忽略 WAL。

## 运行示例

```powershell
dotnet run --project M.Helper.Logging.Sample
Invoke-RestMethod -Method Post http://127.0.0.1:5188/demo/logs
```

打开 <http://127.0.0.1:5188/logs>。示例只监听回环地址，明确允许匿名，此匿名设置不应用于生产环境。数据库在示例输出目录的 `App_Data/sample-logs.db`，保留上次运行数据。

## 测试与打包

```powershell
dotnet test M.Helper.Logging.Test --configuration Release
dotnet build M.Helper.Logging.Sample --configuration Release
dotnet pack M.Helper.Logging --configuration Release --output artifacts/packages
```

测试使用真实临时 SQLite 和本地 HTTP 服务，覆盖并发采集、停机排空、重启恢复、Scope/TraceId、游标分页、过期清理、失败重试、字段限长及授权。
