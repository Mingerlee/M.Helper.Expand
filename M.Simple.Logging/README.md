# M.Simple.Logging

面向 .NET 6 / .NET 8 / ASP.NET Core 的轻量日志包：标准 `ILogger<T>`、本地 SQLite、应用内嵌 Dashboard。无需 Serilog、ORM、前端构建工具或外部日志服务。

同一 NuGet 包包含 `net6.0`、`net8.0` 程序集，消费项目自动选择目标；接入方法一致。源码使用 C# 12，仓库构建需 .NET 8 SDK；使用已打包的 net6.0 程序集不要求升级应用目标框架。

## 快速接入

包发布后安装：`dotnet add package M.Simple.Logging`。仓库内可使用项目引用，当前尚未发布到 NuGet。

```csharp
using M.Simple.Logging;

var builder = WebApplication.CreateBuilder(args);
// 沿用宿主现有的认证方案和授权策略。
builder.Services.AddSimpleLogging();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapSimpleLogDashboard("/logs").RequireAuthorization("LogAdmin");
app.Run();
```

认证方案和 `LogAdmin` 策略由宿主注册。Dashboard 默认要求已登录；生产应用应明确限制管理员权限。注册本包不会清除现有日志提供程序。

宿主使用 `UsePathBase` 时，显式按 `UsePathBase` → `UseRouting` → `UseAuthentication` → `UseAuthorization` 顺序注册中间件，确保 .NET 6 下也正确匹配路由。

.NET 6 的 `MapSimpleLogDashboard` 返回 `SimpleLogDashboardBuilder`，支持对全部端点统一调用 `RequireAuthorization`、`AllowAnonymous`、`WithMetadata`；.NET 8 保持返回 `RouteGroupBuilder`。不要在跨版本代码中显式声明 `RouteGroupBuilder` 或使用仅 .NET 7+ 才提供的路由分组 API。

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
builder.Services.AddSimpleLogging(options =>
{
    options.ApplicationName = "BAMS.Api";
    options.DatabasePath = "App_Data/logs.db";
    options.MinimumLevel = LogLevel.Information;
    options.RetentionDays = 15;
    options.QueueCapacity = 10_000;
    options.BatchSize = 200;
    options.FlushInterval = TimeSpan.FromSeconds(1);
    options.ShutdownTimeout = TimeSpan.FromSeconds(5);
    options.MaxStorageBytes = 512L * 1024 * 1024;
    options.MinFreeDiskSpaceBytes = 256L * 1024 * 1024;
    options.DiskSpaceRecoveryMarginBytes = 64L * 1024 * 1024;
    options.StorageCheckInterval = TimeSpan.FromSeconds(30);
});
```

- 相对路径基于 `AppContext.BaseDirectory`，目录自动创建；启动时检查数据库权限。
- 默认过滤 `Microsoft.AspNetCore` 分类低于 Warning 的框架请求日志，避免页面轮询产生大量自身日志。可通过后续 `builder.Logging.AddFilter<SimpleLoggerProvider>(...)` 覆盖。
- 宿主 Logging 过滤配置仍生效；开启 Debug / Trace 时需要同时允许宿主过滤器和本包 MinimumLevel。
- 字段默认最多 32,768 字符；属性和 Scope 超限保存有效 JSON 截断标识。普通对象通过 `ToString()` 快照，不深度遍历对象。
- `Enrich` 回调可在入队前脱敏消息、异常、属性和 Scope；需线程安全，不得保留并异步修改日志对象。业务主动写入的敏感信息需自行处理。
- 选项在注册时配置，运行期间不要修改；每个宿主仅调用一次 `AddSimpleLogging`。

## 页面与 API

访问 `/logs` 或 `/logs/`，支持宿主 PathBase。所有页面/API 共用路由组授权配置。

- 保留期总数、今日日志、今日错误（含 Critical）、最近一小时。
- 时间、级别、日志来源精确匹配、消息/异常/属性关键词、TraceId 筛选。
- 日志详情、异常堆栈、结构化属性、Scope、复制 JSON、TraceId 关联查询。
- 自动刷新默认关闭，每 10 秒刷新；仅在第一页、详情关闭时生效。
- 当前进程的采集、落库、丢弃、采集失败、写入失败和运行状态，以及日志文件用量、磁盘可用空间、容量清理条数、暂停原因。

| Dashboard 下的相对路径 | 用途 |
| --- | --- |
| `GET /api/logs` | `from/to/level/minimumLevel/category/keyword/traceId/beforeId/pageSize` 筛选 |
| `GET /api/logs/{id}` | 单条详情 |
| `GET /api/statistics?utcOffsetMinutes=480` | 统计，默认 UTC 今日边界 |
| `GET /api/status` | 当前进程状态 |

时间统一存 UTC，范围为 `[from,to)`，建议传携带时区的 ISO 8601。页面今日边界按浏览器当前 UTC 偏移计算。列表按 Id 倒序游标分页，每页默认 50 条、最大 200 条；API 默认不限时间，页面默认最近 24 小时。关键词采用区分大小写的字面量匹配，SQL 参数绑定。

日志内容通过文本 DOM 展示，不当作 HTML 执行；响应禁止缓存，页面不依赖 CDN。

## 容量与磁盘保护

默认开启以下控制，不需要额外依赖：

| 配置 | 默认值 | 行为 |
| --- | --- | --- |
| `RetentionDays` | 15 天 | 分批清理过期日志 |
| `MaxStorageBytes` | 512 MiB | 主文件 + `-wal` + `-shm` 的合计保护阈值，最小 16 MiB |
| `MinFreeDiskSpaceBytes` | 256 MiB | 所在文件系统低于该空间时暂停写入 |
| `DiskSpaceRecoveryMarginBytes` | 64 MiB | 低磁盘暂停后，达到 320 MiB 才恢复，避免反复启停 |
| `StorageCheckInterval` | 30 秒 | 容量维护及暂停后检查恢复的间隔 |

每批写入前检查文件大小和磁盘可用空间，并为当前事务预留增长空间；大批次会按字节预算拆分。空闲时仍检查并自动恢复。容量紧张时按时间从旧到新清理，每次最多 1,000 条，每轮维护最多 10 批；主文件的活跃页清理目标约为总预算的 60%。空间不足时可能删除仍在保留天数内的旧日志。

新库启用 `auto_vacuum=INCREMENTAL`，维护时分批回收空页并截断已完成的 WAL。主文件另设 SQLite 页数限制，约为总预算的 75%，其余为 WAL 和维护预留；达到 SQLite 页数限制或发生 `SQLITE_FULL` 时，当前事务回滚并进入暂停保护。

暂停期间直接丢弃新日志并计入 Dropped，同时释放已排队但尚未提交的批次，不阻塞业务。已提交的历史日志仍可查询。无法读取磁盘空间时也暂停，避免失去保护；长查询阻止 WAL 回收时先等待查询结束，不盲目删除历史。其他普通写入错误仍按原方式重试。

`GET /api/status` 的 `storage` 返回主文件/WAL/SHM 大小、合计用量、上限、可用空间、暂停原因、容量清理数和检查时间。常见原因：`LowDiskSpace`、`DiskSpaceUnavailable`、`StorageLimit`、`CheckpointBusy`、`StorageFull`。这些指标为当前进程的最近检查快照，重启后清理计数归零。

**旧库兼容**：已有 `auto_vacuum=NONE` 的数据库会继续使用空闲页，但不会自动执行需要大量额外空间的完整 VACUUM。若旧主文件本身已超过阈值，会暂停并等待空间释放或提高阈值，不反复删空历史。需要物理缩小时，在维护窗口停应用、备份数据库并确保足够临时空间，再用 SQLite 工具执行：

```sql
PRAGMA auto_vacuum=INCREMENTAL;
VACUUM;
PRAGMA wal_checkpoint(TRUNCATE);
```

Docker 下按数据库目录匹配最长挂载点的可用空间；整个目录需要持久化挂载且可读写。可用空间反映文件系统报告值，不一定等于容器或用户的独立配额。文件阈值属于应用层保护，事务和 WAL 维护期间可能短暂超出；如需严格的磁盘硬配额，应同时在 Docker/宿主存储层配置。不要多实例共用同一 SQLite 文件。

## 可靠性与边界

- 唯一直接 NuGet 依赖为 `Microsoft.Data.Sqlite`，包含传递的 SQLite 原生运行库。其他能力来自对应 .NET 6 / .NET 8 的 `Microsoft.AspNetCore.App` 共享框架，消费端需相应 ASP.NET Core 运行时。
- 单后台写入器、事务批量提交、WAL、`synchronous=NORMAL`，读连接独立。
- 有界队列满时丢弃新日志，不阻塞业务；Formatter / Enrich 失败不抛给业务，计入 CaptureFailures。
- 写入失败保留批次，每秒重试；队列仍有容量上限。状态只暴露错误类型，内部错误不递归写日志。
- 正常停机在默认 5 秒内尽量排空；强制退出、超时、掉电时可能丢失日志，不适用于可靠审计账本。
- 每小时及启动时清理过期记录，每批 1,000 条；容量维护对新库执行增量回收，不自动对旧库执行完整 VACUUM。无磁盘持久缓冲。
- 首版统计扫描保留期数据，关键词扫描筛选范围。建议限制保留期和查询范围；尚未做大规模性能基准。
- 每实例独立本地 SQLite 文件，不放在网络共享目录；容器需挂载持久化目录。首版不做多实例集中查询。
- 备份使用 SQLite 在线备份机制，或停机后备份；不能运行中只复制 `.db` 而忽略 WAL。

## 运行示例

```powershell
dotnet run --project M.Simple.Logging.Sample --framework net8.0
Invoke-RestMethod -Method Post http://127.0.0.1:5188/demo/logs
```

打开 <http://127.0.0.1:5188/logs>。示例只监听回环地址，明确允许匿名，此匿名设置不应用于生产环境。数据库在示例输出目录的 `App_Data/sample-logs.db`，保留上次运行数据。

验证 .NET 6 时将启动参数替换为 `--framework net6.0`。

## 测试与打包

```powershell
dotnet test M.Simple.Logging.Test --configuration Release
dotnet build M.Simple.Logging.Sample --configuration Release
dotnet pack M.Simple.Logging --configuration Release --output artifacts/packages
```

测试使用真实临时 SQLite 和本地 HTTP 服务，覆盖并发采集、停机排空、重启恢复、Scope/TraceId、游标分页、过期清理、失败重试、字段限长、授权、容量清理/缩容、长读事务阻塞 WAL、低磁盘/空间未知暂停与恢复、SQLITE_FULL 回滚及旧库兼容。磁盘不足通过可替换的内部探针模拟，不会实际填满开发机磁盘；Linux Docker 尚未实测。
