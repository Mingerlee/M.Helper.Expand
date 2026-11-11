# 邮件与 FTP

配置、服务注册与实际调用入口。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- services
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Services;
using Microsoft.Extensions.DependencyInjection;

static partial class Demo
{
    public static void Services()
    {
        var services = new ServiceCollection();
        services.AddEMailSetup(_ => new MailSettings
        {
            StmpHostServer = "smtp.example.invalid",
            FromAddress = "sender@example.invalid",
            PortNumber = 587,
            EnableSSL = true,
            RequireAuth = false
        });
        services.AddFtpClientSetup(_ => new FtpClientSettings
        {
            FtpServerIP = "ftp.example.invalid",
            FtpRemotePath = "samples"
        });
        using var provider = services.BuildServiceProvider();
        Check(provider.GetRequiredService<IEMailService>() is EMailService, "邮件服务注册");
        Check(provider.GetRequiredService<IFtpClientService>() is FtpClientService, "FTP 服务注册");
        Console.WriteLine("邮件与 FTP 服务解析成功；仅演示注册，不建立网络连接。");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- AddEMailSetup 注册 MailSettings 和 IEMailService；AddFtpClientSetup 注册 FtpClientSettings 和 IFtpClientService。两类配置与服务在当前实现中均为 Singleton。

- 邮件主机字段名称为 StmpHostServer（历史拼写）；另有 FromAddress、PortNumber、EnableSSL、RequireAuth、Username、Password、DeliveryOption。

- 邮件内容使用 MailContent：ToAddresses、Subject、Body、IsHTML、AttachmentPaths。SendMail 和 SendMailAsync 返回 MailResult，状态字段见 API 参考。

- FTP 的 FtpServerIP 传主机或主机:端口，不带 ftp://；FtpRemotePath 为目录，FtpUrl 属性负责拼接地址。常用方法为 FtpUpload、FtpDownload、GetFtpFileList、GotoFtpDirectory。

- 默认示例只解析服务，不发送邮件、不连接 FTP。异步 FTP 上传下载需传 CancellationToken。FTP 使用传统协议，账号和网络边界由使用方配置。

- GetFtpListDirectoryDetails、FtpDelete、FtpReName、FtpMakeDirectory 等会实际访问远程服务。先准备专用测试目录，再显式调用；不要直接把示例当作生产操作脚本。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
