# M.Simple.Services.Ftp

.NET 8 独立 FTP 服务包，版本 `1.0.0-preview.1`，不依赖 `M.Simple.Expand` 或其他服务包。

```csharp
using M.Simple.Services.Ftp;

services.AddSimpleFtp(options =>
{
    options.FtpServerIP = "ftp.example.com";
    options.FtpUserID = "user";
    options.FtpPassword = password;
    options.FtpRemotePath = "uploads";
});
```

注入 `IFtpClientService`，使用上传、下载、目录管理和文件管理方法。配置工厂入口 `AddFtpClientSetup(provider => settings)` 保留。旧 `FtpClient` 帮助类也迁入此包，使用相同的新命名空间。

服务和配置按 Transient 解析；实例复制配置，当前目录属于该实例。并行多步目录流程应分别解析实例。异步上传、下载、删除支持 `CancellationToken`；取消不能撤销远端已经执行的操作，中断上传可能留下部分文件。

首版保留 `FtpWebRequest` 实现，不包含 SFTP。测试位于根目录 `M.Simple.Services.Ftp.Test`，使用回环协议桩、临时文件及并发测试，不连接外部 FTP。注册示例位于 `M.Simple.Expand/docs/samples/Services.cs.txt`。

```powershell
dotnet test M.Simple.Services.Ftp.Test/M.Simple.Services.Ftp.Test.csproj -c Release
dotnet pack M.Simple.Services.Ftp/M.Simple.Services.Ftp.csproj -c Release -o artifacts/packages
```

原服务命名空间及 `M.Simple.Helper.FtpClient` 改为 `M.Simple.Services.Ftp`。新包只本地打包，不代表公共 NuGet 已发布。
