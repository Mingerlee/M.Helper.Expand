# M.Simple.Logging 接入与使用

通过标准 ILogger<T> 记录日志，使用本地 SQLite 持久化并提供内嵌工作台。宿主需已有认证方案和 LogAdmin 授权策略；无需页面时可省略工作台映射。

当前项目版本 `1.0.1.6`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Logging --version 1.0.1.6
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## Program.cs 配置

```csharp
using M.Simple.Logging; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSimpleLogging(); // 本次新增：注册日志提供程序和 SQLite 存储。

var app = builder.Build();

app.UseSimpleLoggingRequestContext(); // 本次新增（可选）：为错误日志附带脱敏请求上下文。

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSimpleLogDashboard("/logs").RequireAuthorization("LogAdmin"); // 本次新增（可选）：映射受 LogAdmin 策略保护的日志工作台。
app.Run();
```

认证方案及上述授权策略由宿主已有配置提供，示例不代替认证注册。

## 按日志级别和分类筛选

在已有 AddSimpleLogging() 之后、builder.Build() 之前添加。普通日志只记录 Error/Critical，指定分类额外保留 Information：

```csharp
using M.Simple.Logging; // 本次新增：SimpleLoggerProvider 类型。
using Microsoft.Extensions.Logging; // 本次新增：日志级别与筛选扩展。

// 本次新增：设置本包日志提供程序的默认最低级别。
builder.Logging.AddFilter<SimpleLoggerProvider>((_, level) => level >= LogLevel.Error);
// 本次新增：覆盖包内 Microsoft.AspNetCore 默认 Warning 规则。
builder.Logging.AddFilter<SimpleLoggerProvider>("Microsoft.AspNetCore", LogLevel.Error);
// 本次新增：保留缓存管理操作的 Information 日志。
builder.Logging.AddFilter<SimpleLoggerProvider>("M.Simple.Caching.Dashboard", LogLevel.Information);
// 本次新增：为指定 AOP 业务类放行 Information，替换为实际日志分类。
builder.Logging.AddFilter<SimpleLoggerProvider>(
    "M.Simple.Example.Api.Services.ExampleMethodInterceptor", LogLevel.Information);
builder.Logging.AddFilter<SimpleLoggerProvider>(
    "M.Simple.Example.Api.Services.NamedMethodOrderExample", LogLevel.Information);
```

这些规则只影响 SimpleLoggerProvider，不改变 Console 等其他提供程序。ILogger<T> 的分类通常为 T 的完整名称；CreateLogger(name) 则使用传入名称。放行分类不会自动生成日志，业务仍需调用 LogInformation 等方法。

分类按名称前缀匹配，更长、更具体的规则优先；同一提供程序和同长度分类规则采用后注册的规则。因此放在 AddSimpleLogging() 之后，才能覆盖包内 Microsoft.AspNetCore 的默认筛选。

**包内 MinimumLevel 仍是另一道最低级别限制。** 保留默认 Information，才能使用上述分类放行；若 options.MinimumLevel 为 Error，分类允许 Information 也不会保存。需要 Debug/Trace 时，包内 MinimumLevel 和宿主筛选必须同时允许。

### appsettings.json 配置替代写法

可使用标准 ASP.NET Core Logging 配置为提供程序别名 Simple 设置筛选。与上方代码选择一种方式管理同一批规则：

```json
{
  "Logging": {
    "Simple": {
      "LogLevel": {
        "Default": "Error",
        "Microsoft.AspNetCore": "Error",
        "M.Simple.Caching.Dashboard": "Information",
        "M.Simple.Example.Api.Services.ExampleMethodInterceptor": "Information",
        "M.Simple.Example.Api.Services.NamedMethodOrderExample": "Information"
      }
    }
  }
}
```

Simple 对应本包的 ProviderAlias。此配置不自动绑定 DatabasePath、DashboardTitle 或请求上下文开关；业务类的分类名称按实际项目替换。

## 使用说明

日志目录需要写入权限，相对路径基于 AppContext.BaseDirectory。注册不会清除宿主已有日志提供程序。请求采集为可选功能；不采集请求头、Cookie 或响应体。与统一响应包协作去重需核对两个实际安装版本；Response 1.0.0.1 不应直接视为具备 README 中标注的 1.0.1 去重能力。

## 自动附带错误请求参数（可选）

```csharp
builder.Services.AddSimpleLogging();
var app = builder.Build();
// 已有 UseExceptionHandler 或业务异常处理中间件应放在这里，包会将异常继续向外抛出。
app.UseSimpleLoggingRequestContext(options =>
{
    options.MaxBodyBytes = 16 * 1024;
    options.ShouldCapture = context => context.Request.Path.StartsWithSegments("/api");
    options.SensitiveFields.Add("IdentityNumber");
});
// 之后是认证、授权、Controller/接口等原有配置。
```

仅调用 `AddSimpleLogging` 不启用采集。安装 `UseSimpleLoggingRequestContext` 后，在接口执行范围内调用的 `LogError` / `LogCritical` 自动将请求信息放入 `PropertiesJson.Request`，在工作台详情的“结构化属性”查看；Information / Warning 等日志不附带参数。

- 包含 Method、Path（不含原始 QueryString）、Query、RouteValues、RequestId、TraceId、Body 和 BodyStatus；已有业务结构化属性保留，`Request` 为本包保留字段。
- JSON（含 `+json`）和 URL 编码表单在接口执行前有界读取，并复位到原位置，保持 Controller / Minimal API 参数绑定。即使最后没有错误，也会发生这次有界读取，但不会额外生成日志。
- 默认最多读取 16 KiB；已知超大或读取超过上限时不保存请求体，标记 `TooLarge`。无效或深度超过 32 层的 JSON 标记 `UnavailableOrInvalid`，不存原始文本；其他类型（如二进制、纯文本、压缩请求体）跳过。解压中间件需要先于本中间件执行。
- JSON 嵌套字段、Query、路由及表单字段按名称脱敏，不区分大小写，忽略标点；默认覆盖含 password、passwd、pwd、token、secret、authorization、cookie、apikey 的名称，可通过 `SensitiveFields` 追加。仅按字段名称处理，不能识别普通字符串内容里嵌入的密码，路径本身也不做业务语义脱敏；不应把秘密放进 URL 路径或无名称字符串。
- multipart 文件上传不预读。错误发生时如果下游已解析表单，则记录脱敏字段及文件名/大小，不读取文件内容；下游尚未解析时标记 `FormNotParsed`。采集不会主动触发表单解析。
- `IncludeBody=false` 可只记录路径、Query 等元数据；`ShouldCapture` 可排除登录、上传等接口。不采集任何请求头，Authorization 和 Cookie 不进入请求快照。
- 请求上下文也使用 `MaxBodyBytes` 作为 JSON 字节预算，超限时省略 Body/Query/RouteValues，并标记 `ContextTooLarge`；集合和字段还有单项限制。日志原有的 `MaxFieldLength` 总属性长度限制仍生效，过小可能使整个 PropertiesJson 变为截断标记。
- LogUnhandledExceptions=true 默认记录未处理异常并重新抛出。跨包去重是否可用需核对实际安装的统一响应版本，不能仅因启用请求采集就认定已去重。
- 仅返回 HTTP 500 而没有错误日志或异常，不自动生成日志。后台任务在请求结束后不再携带该请求上下文。

请求缓冲使用 ASP.NET Core 的 `EnableBuffering`；服务本身的请求体大小、表单和上传限制仍由宿主配置。本功能不是审计记录，不改变磁盘容量保护策略。

### 与统一响应包配合

```csharp
builder.Services.AddSimpleLogging();
builder.Services.AddUnifiedResponse();
var app = builder.Build();
app.UseUnifiedResponse();
app.UseSimpleLoggingRequestContext();
```

需引用 M.Simple.AspNetCore.Response 1.0.1 或更高兼容版本，并导入对应命名空间。本包 1.0.1.5 起记录未处理异常后，会在当前 HttpContext.Items 中标记该异常引用；响应包跳过相同异常的自动日志，继续返回统一 500。

此标记仅针对自动记录的未处理异常。业务主动写入的错误、其他请求和其他异常不会被合并。请求上下文与脱敏规则不变；单独使用本日志包仍保持记录异常并重新抛出的原有行为。两包一起使用时，无需通过关闭 LogUnhandledExceptions 来去重。

### 按单条错误日志开启或关闭

引入 `using M.Simple.Logging;` 后，可以在标准 `LogError` 的参数前加 `includeRequestContext`：

```csharp
logger.LogError(true, "订单 {OrderId} 保存失败", orderId);   // 本条附带请求参数
logger.LogError(false, "订单 {OrderId} 校验失败", orderId); // 本条不附带请求参数
logger.LogError(true, exception, "订单 {OrderId} 保存异常", orderId);
logger.LogError(false, new EventId(2001, "SaveFailed"), exception,
    "订单 {OrderId} 保存异常", orderId);
```

支持消息、异常、EventId、EventId + 异常四种重载，保留消息模板与结构化属性。开关仅影响当前这一条日志，不影响下一条、并发请求或其他日志提供程序。

如果多数地方不需要附带，可以关闭全局默认，再由个别 `LogError(true, ...)` 开启：

```csharp
app.UseSimpleLoggingRequestContext(options =>
{
    options.IncludeInErrorLogs = false;
});

logger.LogError("普通错误");                  // 跟随全局默认，不附带
logger.LogError(true, "需要请求上下文的错误"); // 单独开启
logger.LogError(false, "无需请求上下文的错误"); // 单独关闭
```

默认 `IncludeInErrorLogs=true`，原有 `LogError(...)` / `LogCritical(...)` 写法保持自动附带；当前单条重载仅为 `LogError` 提供。该开关控制是否写入请求快照，不控制错误日志本身是否生成。

单条开启仍需安装请求中间件并处于请求执行范围；不会突破 `ShouldCapture` 路径排除、`IncludeBody=false`、大小限制或脱敏规则。`IncludeInErrorLogs=false` 仍会准备有界请求快照，以便单条调用开启；如果整个应用都不需要采集，不安装中间件即可。


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



## 接入注意事项

日志目录需要写入权限，相对路径基于 AppContext.BaseDirectory。注册不会清除宿主已有日志提供程序。请求采集为可选功能；不采集请求头、Cookie 或响应体。与统一响应包协作去重需核对两个实际安装版本；Response 1.0.0.1 不应直接视为具备 README 中标注的 1.0.1 去重能力。

## 浏览器标签页标题

通过 DashboardTitle 配置日志工作台的浏览器标题；未配置、null 或空白时使用默认值 `Logging Dashboard`。此选项不改变页内标题或日志中的 ApplicationName。

```csharp
builder.Services.AddSimpleLogging(options =>
{
    options.DashboardTitle = "业务日志"; // 本次新增：自定义浏览器标签页标题。
});
```

将配置合并到项目已有的 AddSimpleLogging 调用中，不要重复注册。标题按文本编码后输出。该选项从 1.0.1.6 起提供，也可使用当前源码引用。

## 单独控制未处理异常的上下文

普通错误默认不附带上下文，但未处理异常的自动日志附带上下文：

```csharp
app.UseSimpleLoggingRequestContext(options =>
{
    options.IncludeInErrorLogs = false;
    options.LogUnhandledExceptions = true;
    options.IncludeContextInUnhandledExceptions = true; // 本次新增：仅开启未处理异常自动日志的上下文。
});
```

IncludeContextInUnhandledExceptions 为可空布尔值：true 开启、false 关闭，未配置（null）跟随 IncludeInErrorLogs，保持原行为。它只影响事件号 1001 的自动异常日志，不改变普通 Error/Critical 或单条日志开关。配置合并到已有的请求中间件调用中。

上下文位于日志详情的结构化属性 Request；仍遵循 ShouldCapture 路径筛选、IncludeBody、脱敏与大小限制。LogUnhandledExceptions=false 时不生成自动异常日志，单独开启上下文不会额外生成日志。中间件需位于宿主异常处理之后、接口执行之前，使其能够捕获异常；采用统一响应时推荐 UseUnifiedResponse() → UseSimpleLoggingRequestContext() → 接口执行。该选项从 1.0.1.6 起提供，也可使用当前源码引用。

## 业务日志使用示例

业务类通过构造函数注入 ILogger<T>，不需要自行创建日志提供程序：

```csharp
using Microsoft.Extensions.Logging;
using M.Simple.Logging;

public sealed class OrderAuditService(ILogger<OrderAuditService> logger)
{
    public void RecordCreated(long orderId)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OrderId"] = orderId
        });
        logger.LogInformation("订单 {OrderId} 创建成功", orderId);
    }

    public void RecordFailure(long orderId, Exception exception)
    {
        logger.LogError(true, exception, "订单 {OrderId} 保存失败", orderId);
    }
}
```

在 Build() 前注册 AddScoped<OrderAuditService>()，由现有业务流程注入并调用 RecordCreated / RecordFailure。方法内不再抛出或吞掉业务异常，异常传播由调用方决定。单条 true 仅在已安装请求采集中间件、当前请求允许采集时附带上下文；后台调用仍可写日志，但不会凭空生成请求上下文。Information 需满足前述分类筛选和 MinimumLevel 才会保存。登录具备 LogAdmin 权限的账号后访问 /logs，按 OrderAuditService 完整分类或 OrderId 关键词查询记录。
