# M.Simple.Expand 服务拆分

本次沿用仓库根目录的平铺结构，仅新增：

```text
M.Simple.Services.DependencyInjection/
M.Simple.Services.DependencyInjection.Test/
M.Simple.Services.Email/
M.Simple.Services.Email.Test/
M.Simple.Services.Ftp/
M.Simple.Services.Ftp.Test/
```

现有项目保持原位，不创建 packages、legacy 或解决方案分组文件夹。EF Core、日志和旧 M.Helper.Expand 项目不做调整。

## 版本与迁移

`M.Simple.Expand` 移除服务后的版本为 `3.0.0-preview.1`，三个新服务包为 `1.0.0-preview.1`。这是公开类型和命名空间的破坏性变更，旧项目可以暂时固定原 2.x 包；升级时安装所需服务包，修改 using 并重新编译。

| 原内容 | 新包及命名空间 |
| --- | --- |
| `EMailService`、`IEMailService`、Mail 配置、内容、结果和枚举 | `M.Simple.Services.Email` |
| `FtpClientService`、`IFtpClientService`、`FtpClientSettings` | `M.Simple.Services.Ftp` |
| `M.Simple.Helper.FtpClient` | `M.Simple.Services.Ftp.FtpClient` |
| `AutowiredService`、`AppServiceAttribute`、`AutowiredAttribute` | `M.Simple.Services.DependencyInjection` |

原有 `AddEMailSetup`、`AddFtpClientSetup`、`AddAppServices` 方法名保留，注册扩展迁入对应服务包。增加 `AddSimpleEmail`、`AddSimpleFtp` 配置委托入口，以及仅注册手动装配服务的 `AddSimpleDependencyInjection`。

三个服务包互不依赖，也不依赖 `M.Simple.Expand`。邮件地址解析改为包内实现，保留原逗号和分号分隔规则。原公共字符串扩展仍在通用包中。`AppSetting` 与配置扩展仍保留在通用包。

## 生命周期变化

- 邮件服务及配置由 Singleton 改为 Transient，配置工厂每次解析调用，可读取当前作用域依赖。
- FTP 服务仍为 Transient，配置工厂改为每次解析调用；实例配置副本和目录隔离行为保留。
- `AutowiredService` 为 Scoped，需要从当前作用域解析。`AppServiceAttribute` 的默认 Lifetime 仍为 Singleton，有作用域依赖的业务服务应显式选择 Scoped。
- 推荐 `AddAppServices(typeof(业务类型).Assembly)` 显式指定程序集；无参数入口仍扫描当前已加载程序集。重复扫描不会重复注册同实现。程序集加载失败会直接报错。
- `[Autowired]` 本身不会自动执行属性注入，仍需主动调用 `AutowiredService.Autowired`。

本次保留原 SMTP 和 FTP 底层实现以及资源释放、下载临时文件、异步取消等行为，不引入自动重试。

## 构建与验证

```powershell
dotnet build M.Helper.Expand.sln -c Release
dotnet test M.Helper.Expand.sln -c Release --no-build
dotnet run --project M.Simple.Expand/docs/samples/M.Simple.Expand.Samples.csproj -c Release -- all
dotnet pack M.Simple.Services.Email/M.Simple.Services.Email.csproj -c Release -o artifacts/packages
```

综合示例项目引用三个新服务项目；生产通用包没有这些依赖。邮件示例只检查注册和解析，不发送外部邮件；FTP 示例不连接外部服务器。

包只在本地构建，不自动发布到公共 NuGet，也不自动提交或推送 Git。
