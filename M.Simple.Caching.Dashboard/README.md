# M.Simple.Caching.Dashboard

.NET 8 嵌入式缓存管理页面，项目版本 **1.0.0.4**。支持 Memory、Redis、MongoDB，复用宿主认证授权，无需单独部署前端。本版扩展为四种管理范围，默认保持当前应用范围。All 的含义已变更为全部数据，原 All 行为改用 AllManaged。

## 安装与接入

安装 Dashboard 和所选后端包。Dashboard 自动依赖核心包；使用内存时无需额外安装其他缓存包。使用 Redis/MongoDB 时，还需安装对应后端包。配套核心及 MongoDB 后端版本为 1.0.0.4，Redis 后端为 1.0.0.5（支持 Redis 3.2+），不能搭配缺少管理接口的旧版 DLL。

```powershell
dotnet add package M.Simple.Caching.Dashboard --version 1.0.0.4
# Redis 后端另加 M.Simple.Caching.Redis --version 1.0.0.5；MongoDB 后端另加 M.Simple.Caching.MongoDB --version 1.0.0.4。
```

```csharp
using M.Simple.Caching;
using M.Simple.Caching.Dashboard;

// 使用项目已有的认证方案（Cookie/JWT 等）。
builder.Services.AddAuthorization(options =>
    options.AddPolicy("CacheAdmin", policy => policy.RequireRole("CacheAdmin")));

builder.Services.AddSimpleMemoryCache(options =>
{
    options.KeyPrefix = "MyApi:Production:"; // 默认 CurrentApplication 管理模式要求非空应用前缀
});
builder.Services.AddSimpleCacheDashboard(options =>
{
    options.ReadOnly = false;
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapSimpleCacheDashboard("/cache").RequireAuthorization("CacheAdmin");
```

浏览器访问 /cache/。页面、脚本、样式及全部 API 默认要求登录，可在返回的路由组增加权限策略。Cookie 登录适合浏览器直接访问；JWT 宿主需要让页面请求能够通过宿主认证（例如宿主认证 Cookie 或代理），页面不保存或拼接访问令牌。如需匿名访问，可显式调用 `app.MapSimpleCacheDashboard("/cache").AllowAnonymous()`；只读面板可同时设置 `ReadOnly = true`，删除及预览接口仍返回 403。未配置 AllowAnonymous 时继续要求登录。

Redis/MongoDB 项目将 AddSimpleMemoryCache 替换为对应后端注册，不要同时注册两个后端。仅使用业务缓存、不接管理页面时，原有注册方式不受影响。自定义后端需要额外实现 ISimpleCacheAdminStore。

## 第一版功能与范围

- 运行概览：后端、前缀、当前实例、命中率、命中/未命中、工厂加载/失败次数。
- Key 前缀搜索、游标分页、JSON 详情、大小和过期策略。
- 单条/批量删除，每次最多 200 个 Key。列表及详情读取不续期，也不增加命中/未命中计数。
- 前缀清理先生成最多 200 个 Key 的预览；两分钟内、同一认证用户、一次性确认后删除该快照中的 Key。
- 预览最多扫描 20 页，部分结果明确显示为 partial；大范围清理可重复预览。不执行 Redis FLUSHDB、KEYS 或 MongoDB 整库清空。
- 最近 100 条管理操作保存在当前进程，同时通过 ILogger 记录操作人、动作、Key 和结果，不记录缓存值。
- ReadOnly=true 在服务端禁止删除、预览和确认接口；页面隐藏删除按钮。
- 缓存值和 Key 使用文本显示，外部脚本/样式为程序集嵌入资源，不依赖 CDN。

默认 CurrentApplication 模式要求非空 KeyPrefix，且应专属于本应用/环境；嵌套前缀也属于该管理范围。该模式的管理接口使用去掉 KeyPrefix 后的相对 Key。Prefixes、All、AllManaged 模式使用完整 Key，详见下节；Key 不具备路径语义。

## 管理范围配置

管理范围与业务 KeyPrefix 独立，不改变 ISimpleCache 的业务读写行为。内存、Redis、MongoDB 使用同一套范围规则。

```csharp
using M.Simple.Caching;
using M.Simple.Caching.Dashboard;

// 四种方式只选一种注册；不配置 Scope 时保持原有应用范围。
builder.Services.AddSimpleCacheDashboard();

// 指定完整存储前缀，多个前缀取并集；搜索条件与这个并集取交集。
builder.Services.AddSimpleCacheDashboard(options =>
{
    options.Scope = CacheDashboardScope.Prefixes;
    options.Prefixes = ["Bams:Excel:", "Bams:Import:"];
});

// 当前后端存储范围内的全部数据，不要求本包格式。
builder.Services.AddSimpleCacheDashboard(options => options.Scope = CacheDashboardScope.All);

// 所有应用通过本包存入的有效缓存（旧版 All 的行为）。
builder.Services.AddSimpleCacheDashboard(options => options.Scope = CacheDashboardScope.AllManaged);
```

| Scope | 管理范围 | 页面及 API 的 Key / 搜索前缀 |
|---|---|---|
| CurrentApplication（默认） | 当前业务 KeyPrefix | 相对 Key / 相对前缀 |
| Prefixes | 全部数据中匹配任一完整前缀的数据 | 完整 Key / 完整前缀 |
| All | 当前后端存储范围内全部数据，包含非本包数据 | 完整 Key / 完整前缀 |
| AllManaged | 所有应用符合本包格式的有效缓存 | 完整 Key / 完整前缀 |

All 对内存是当前缓存存储实例，对 Redis 是配置的逻辑数据库（集群为配置可访问的主节点），对 MongoDB 是配置的数据库与集合。All、Prefixes 可读取 Redis 原生数据和 MongoDB 普通文档；AllManaged、CurrentApplication 只读取符合本包格式且未过期的缓存。不会搜索其他 MongoDB 集合或其他 Redis 逻辑库。内存后端是本包独立存储实例，不包含宿主其他 IMemoryCache 实例的条目。

列表、详情、单条/批量删除、前缀预览清理都校验同一范围。越界查询返回空列表，越界详情/删除返回 400；批量删除先检查全部 Key 的范围，再开始逐项删除。游标绑定管理模式、完整前缀集合、搜索条件及后端，改变范围或搜索后必须重新查询。

Prefixes 必须提供 1–64 个非空前缀，单个长度不超过 512；比较区分大小写、按字面匹配。重复或嵌套前缀会归并，多分区游标不会因重叠配置重复显示数据。其他模式不得配置 Prefixes，配置无效时注册即报错。CurrentApplication 继续要求非空业务 KeyPrefix；其他模式允许业务前缀为空。

搜索留空表示当前管理范围内全部有效项。All、AllManaged 模式按前缀清理必须输入非空的完整 Key 前缀，页面和服务端同时限制；仍需预览后确认，不提供清空存储命令。All、Prefixes 可查看、删除范围内的普通业务数据；AllManaged 可管理其他应用的本包缓存，应限定管理员权限。页面顶部会显示实际模式和范围。

不接页面、直接使用 ISimpleCacheAdmin 时，可以在后端注册之外调用下面的核心配置方法；接页面时统一在 AddSimpleCacheDashboard 中配置即可：

```csharp
builder.Services.AddSimpleCacheManagement(options =>
{
    options.Scope = CacheDashboardScope.Prefixes;
    options.Prefixes = ["Bams:Excel:"];
});
```

配置在注册及创建管理实例时取快照，运行时修改配置数组不会扩大管理范围。此功能要求核心包 1.0.0.4 和 Dashboard 1.0.0.3；Redis/MongoDB 使用上方列出的配套版本。旧包不包含这些配置项。

内存只管理当前实例。Redis 使用原生 SCAN（COUNT 是服务端提示值）遍历已连接主节点，优先使用 UNLINK 删除，Redis 3.2 不支持该命令时使用 DEL；MongoDB 按 BSON _id 顺序进行游标分页，支持混合类型的 _id；仍排除本包逻辑过期缓存，普通文档不解释其业务过期字段。列表不传输值；Redis String 显示字节数，集合类型显示项数；MongoDB 普通文档显示 BSON 字节大小，本包条目显示 payload 字节数。

所有扫描是实时、尽力结果，不是事务快照；并发写入、淘汰或 Redis 拓扑变化可能造成重复/遗漏或需重查。页面不展示误导性的全局总数。过期时间采用服务端 UTC，浏览器按本地时间显示。

## 认证、防伪与操作记录

宿主需配置有效认证方案。所有删除接口使用 POST 和 ASP.NET Core Antiforgery 校验（包括 JWT 用户），页面自动获取令牌及 Cookie，并读取宿主 Antiforgery HeaderName。宿主如禁用了 HeaderName，需启用请求头令牌。请将页面放在同源地址下，并在部署环境使用 HTTPS。

多实例部署应共享 Data Protection 密钥；预览和最近操作记录在当前进程内，确认清理需回到生成预览的实例（会话粘滞）。实例重启或预览丢失时重新预览即可。

接入 M.Simple.Logging 后，管理操作随 ILogger 自动持久化：

```csharp
builder.Services.AddSimpleLogging();
// 可选映射日志页，并使用同一管理员策略
app.MapSimpleLogDashboard("/logs").RequireAuthorization("CacheAdmin");
```

Dashboard 自身不会重复写另一套持久日志。最近操作 API 为当前实例共享的管理员记录，不包含业务缓存值。

## 指标定义与删除语义

指标为当前 ISimpleCache 单例从创建起累计，不跨实例汇总。Hits/Misses 仅统计成功完成的 TryGetAsync 和 GetOrCreateAsync 首次查询；内部二次查询、ExistsAsync 和管理查询不计入。Loads 为实际工厂调用数，LoadFailures 包含工厂加载流程异常/超时，Writes 为成功提交到后端的写操作（内存可能拒绝或淘汰），Removes 为业务删除请求成功数及管理端实际删除数。

管理删除复用本地写锁并使此前同 Key 的待加载工厂失效，防止本实例旧工厂晚到后重新写入。跨实例工厂不受此锁控制，仍可能重新填充；本包不提供分布式锁或数据库事务。批量删除逐项执行，失败前的删除不会回滚，操作记录可定位已处理的 Key。预览后被其他请求更新的同名 Key 也会被删除。

## API

全部相对于映射路径：

| 方法 | 路径 | 用途 |
|---|---|---|
| GET | /api/status | 当前后端、范围及实例统计 |
| GET | /api/entries?prefix=product:&pageSize=50&cursor=... | 列表；页大小 1–200，cursor 不透明且绑定管理范围和搜索前缀 |
| GET | /api/entry?key=product:1 | 不续期读取 JSON 详情 |
| GET | /api/token | 获取防伪令牌及请求头名称 |
| GET | /api/operations | 最近操作 |
| POST | /api/remove | {"keys":["product:1"]} |
| POST | /api/preview | {"prefix":"product:"} |
| POST | /api/clear | {"token":"预览返回的令牌"} |

搜索前缀为空表示当前管理范围内所有匹配项，不会越过该范围；All、AllManaged 模式的清理预览不接受空白前缀。清理仍需预览确认，单次上限不变。无效参数为 400，无权限为 401/403（Cookie 宿主可重定向登录），无详情为 404，失效/重放预览为 409，后端不可用为 503，响应不暴露连接串。

## 示例与验证

见 [Web 示例](../M.Simple.Caching.Dashboard.Sample/README.md)。

```powershell
dotnet test M.Simple.Caching.Test/M.Simple.Caching.Test.csproj -c Release
dotnet pack M.Simple.Caching.Dashboard/M.Simple.Caching.Dashboard.csproj -c Release -o artifacts/nuget
```

Redis/MongoDB 集成测试需设置 M_SIMPLE_TEST_REDIS 和 M_SIMPLE_TEST_MONGODB。未配置时相应测试报告未执行。本仓库不会自动发布 NuGet 包。

## 1.0.0.3 原生数据详情与升级

- Redis：列表支持全部原生类型。String 详情最多读取前 65536 字节；Hash、List、Set、SortedSet 的预览最多 100 个值、单值最多 4096 字节；Stream（Redis 5+）预览最多 10 条记录且最多 100 个值。Hash/SortedSet 分别以字段/值、成员/分数对排列；Stream 按记录 ID 与字段/值排列。未知模块类型显示元信息，不读取模块值。
- Redis 详情同时提供 UTF-8 text 和 base64；非 UTF-8 二进制的 text 为 null。截断以 truncated=true 标记。Hash/Set 使用扫描预览，返回样本不代表全部内容，查询不修改 TTL。
- MongoDB：普通文档以 Extended JSON 展示；超过 65536 BSON 字节时只返回 _id、大小和 previewOmitted=true，不传输整个大文档。非字符串、空白字符串、以保留前缀 ~mongo: 开头的 _id 会生成不透明管理 Key；详情和删除必须原样传回该 Key，保持 BSON 类型。前缀筛选针对数据库中的字符串 _id，非字符串 _id 可在 All 模式查询。
- 原生数据在 All、Prefixes 模式也可单条/批量删除、预览清理；CurrentApplication、AllManaged 不允许通过完整 Key 绕过本包格式检查。删除仍只作用于当前配置的逻辑库或集合，不改变业务缓存的序列化格式。
- 升级：核心及 MongoDB 使用 1.0.0.4、Redis 使用 1.0.0.5、Dashboard 使用 1.0.0.3。默认配置行为不变；此前显式设置 Scope.All 的项目应确认是要管理全部原生数据，还是改成 Scope.AllManaged 保留旧行为。
