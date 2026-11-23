# M.Helper.Expand

新增 **[M.Simple.Container](M.Simple.Container/README.md)**：.NET 8 独立依赖注入容器，一行 Host 接入，支持程序集批量注册、按接口筛选、三种生命周期及同步/异步释放。[Web 示例](M.Simple.Container.Sample/README.md)，性能比较见 `M.Simple.Container.Benchmarks`。

新增 **[M.Simple.Services.Interception](M.Simple.Services.Interception/README.md)**：接口 AOP 代理，支持同步/异步方法前后钩子、方法与类型特性筛选及 DI 生命周期。[可运行示例](M.Simple.Services.Interception.Sample/README.md)。1.0.0.3 支持前置修改参数、后置替换返回值；1.0.0.2 起支持按方法名指定前后置特性。

新增 **[M.Simple.Caching.Dashboard](M.Simple.Caching.Dashboard/README.md)**：支持内存、Redis、MongoDB 的缓存管理页面，提供前缀搜索、JSON 详情、删除预览、权限控制和操作日志。[Web 示例](M.Simple.Caching.Dashboard.Sample/README.md)。

新增 **[M.Simple.Caching](M.Simple.Caching/README.md)**：.NET 8 统一缓存接口、内存缓存与进程内异步加载合并；可选 [Redis](M.Simple.Caching.Redis/README.md) 或 [MongoDB](M.Simple.Caching.MongoDB/README.md) 后端，安装对应包并注册一次即可。[可运行示例](M.Simple.Caching.Sample/README.md)。

新增 **[M.Simple.AspNetCore.Response](M.Simple.AspNetCore.Response/README.md)**：.NET 8 MVC 统一返回 `code/message/data`、参数校验与异常处理；可选 [Swagger 适配包](M.Simple.AspNetCore.Response.Swagger/README.md)。通过 `AddUnifiedResponse()` 和 `UseUnifiedResponse()` 接入。

新增独立服务包：[邮件](M.Simple.Services.Email/README.md)、[FTP](M.Simple.Services.Ftp/README.md)、[自动注入](M.Simple.Services.DependencyInjection/README.md)。源码与测试沿用根目录平铺结构，升级步骤见[迁移说明](MIGRATION.md)。

新增 **[M.Simple.EntityFrameworkCore](M.Simple.EntityFrameworkCore/README.md)** 首版：基础 CRUD、DTO 分页、工作单元及 MySQL / SQL Server 原生批量新增。按需安装数据库适配包，核心包自动引入；[可运行示例](M.Simple.EntityFrameworkCore.Sample/README.md)。

新增独立日志包 **[M.Simple.Logging](M.Simple.Logging/README.md)**：支持 .NET 8 标准日志接口、本地 SQLite 和内嵌 Dashboard。快速体验：`dotnet run --project M.Simple.Logging.Sample --framework net8.0`，访问 <http://127.0.0.1:5188/logs>。

本仓库当前维护的通用扩展包为 **M.Simple.Expand**，目标框架为 .NET 8，供多个 .NET 项目复用，也可构建为 NuGet 包。

- [包说明与快速示例](M.Simple.Expand/README.md)
- [完整使用手册](M.Simple.Expand/docs/usage.md)
- [可运行示例](M.Simple.Expand/docs/samples/README.md)
- [API 与源码索引](M.Simple.Expand/docs/api.md)
- [本地 IIS 手册站点](http://127.0.0.1:8091/)（需先在本机生成并挂载）

## 构建与测试

在仓库根目录运行：

```powershell
dotnet build M.Simple.Expand/M.Simple.Expand.csproj --configuration Release
dotnet test M.Simple.Expand.Test/M.Simple.Expand.Test.csproj --configuration Release
```

安装、功能分类、日期计算约定、FTP 生命周期及文档站点更新步骤见[包 README](M.Simple.Expand/README.md)。

## 打包并发布 NuGet

在仓库根目录使用 `Publish-NuGet.ps1`，指定项目目录或 csproj 路径。脚本自动读取项目版本、生成 Release 包，并将本次生成的包推送至 nuget.org；无需手动修改包文件名或路径。不会自动增加版本号，也不会自动发布项目依赖，请先发布需要更新的依赖包。

```powershell
# 首次配置或更换密钥：交互输入，Windows 当前用户加密保存，不进入 Git
.\Publish-NuGet.ps1 -SaveApiKey

# 打包完成后自动推送
.\Publish-NuGet.ps1 M.Simple.Caching.Redis
.\Publish-NuGet.ps1 M.Simple.Logging

# 多包按指定顺序打包，全部打包成功后再按顺序推送
.\Publish-NuGet.ps1 -Project M.Simple.Caching,M.Simple.Caching.Redis

# 只打包，或自定义输出文件夹（相对路径以仓库根目录为基准）
.\Publish-NuGet.ps1 M.Simple.Caching.Redis -PackOnly
.\Publish-NuGet.ps1 M.Simple.Logging -OutputDirectory artifacts/nuget
```

默认输出目录为 `artifacts/packages`；只推送本次生成的 `.nupkg`，已有版本通过 `--skip-duplicate` 跳过。密钥也可通过环境变量 `NUGET_API_KEY` 提供，优先于本地保存的密钥。本地加密密钥位于 `artifacts/private/nuget-api-key.txt`，仅当前 Windows 用户可解密；不要将密钥写入脚本或 README。
