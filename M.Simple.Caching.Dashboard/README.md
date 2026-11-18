# M.Simple.Caching.Dashboard

.NET 8 嵌入式缓存管理页面，源码版本 **1.0.0**。支持 Memory、Redis、MongoDB，复用宿主认证授权，无需单独部署前端。

## 安装与接入

安装 Dashboard 和所选后端包。Dashboard 自动依赖核心包；使用内存时无需额外安装其他缓存包。使用 Redis/MongoDB 时，还需安装对应后端包。配套核心及后端版本为 1.0.0.2，不能搭配旧版 DLL。

```powershell
dotnet add package M.Simple.Caching.Dashboard --version 1.0.0
# Redis 后端另加 M.Simple.Caching.Redis --version 1.0.0.2；MongoDB 后端另加 M.Simple.Caching.MongoDB --version 1.0.0.2。
```

```csharp
using M.Simple.Caching;
using M.Simple.Caching.Dashboard;

// 使用项目已有的认证方案（Cookie/JWT 等）。
builder.Services.AddAuthorization(options =>
    options.AddPolicy("CacheAdmin", policy => policy.RequireRole("CacheAdmin")));

builder.Services.AddSimpleMemoryCache(options =>
{
    options.KeyPrefix = "MyApi:Production:"; // 管理功能强制要求非空应用前缀
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

浏览器访问 /cache/。页面、脚本、样式及全部 API 默认要求登录，可在返回的路由组增加权限策略。Cookie 登录适合浏览器直接访问；JWT 宿主需要让页面请求能够通过宿主认证（例如宿主认证 Cookie 或代理），页面不保存或拼接访问令牌。不要对管理路由使用 AllowAnonymous。

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

非空 KeyPrefix 必须专属于本应用/环境，不能复用其他应用的前缀；嵌套前缀也属于该管理范围。管理接口的 Key 均为去掉 KeyPrefix 后的相对 Key，不具备路径语义。

内存只管理当前实例。Redis 使用原生 SCAN（COUNT 是服务端提示值）遍历已连接主节点，使用 UNLINK 删除；MongoDB 按 _id 游标分页，始终排除逻辑过期文档，即使 TTL 清理尚未发生。列表不传输 JSON 值，Redis 在服务器计算字节数，MongoDB 使用 $binarySize 投影。

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
| GET | /api/entries?prefix=product:&pageSize=50&cursor=... | 列表；页大小 1–200，cursor 不透明且绑定搜索前缀 |
| GET | /api/entry?key=product:1 | 不续期读取 JSON 详情 |
| GET | /api/token | 获取防伪令牌及请求头名称 |
| GET | /api/operations | 最近操作 |
| POST | /api/remove | {"keys":["product:1"]} |
| POST | /api/preview | {"prefix":"product:"} |
| POST | /api/clear | {"token":"预览返回的令牌"} |

前缀为空表示已配置 KeyPrefix 内所有匹配项，不会越过该范围。清理仍需预览确认，单次上限不变。无效参数为 400，无权限为 401/403（Cookie 宿主可重定向登录），无详情为 404，失效/重放预览为 409，后端不可用为 503，响应不暴露连接串。

## 示例与验证

见 [Web 示例](../M.Simple.Caching.Dashboard.Sample/README.md)。

```powershell
dotnet test M.Simple.Caching.Test/M.Simple.Caching.Test.csproj -c Release
dotnet pack M.Simple.Caching.Dashboard/M.Simple.Caching.Dashboard.csproj -c Release -o artifacts/nuget
```

Redis/MongoDB 集成测试需设置 M_SIMPLE_TEST_REDIS 和 M_SIMPLE_TEST_MONGODB。未配置时相应测试报告未执行。本仓库不会自动发布 NuGet 包。
