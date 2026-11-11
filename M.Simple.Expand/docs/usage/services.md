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

- 邮件地址分别设置 ToAddresses（收件人）、CcAddresses（抄送）、BccAddresses（密送）、ReplyToAddresses（回复地址），各列表独立读取，支持逗号或分号分隔多个地址。可选地址为 null 或空字符串时不添加对应地址；回归测试只检查内存中的地址转换，不发送邮件。

- FTP 的 FtpServerIP 传主机或主机:端口，不带 ftp://；FtpRemotePath 为目录，FtpUrl 属性负责拼接地址。常用方法为 FtpUpload、FtpDownload、GetFtpFileList、GotoFtpDirectory。

- FtpClient.Download、FtpClientService.FtpDownload 和 FtpDownloadAsync 先写入目标目录的临时文件，完整下载并成功关闭流后才替换目标文件。连接失败、远端拒绝、传输失败或异步取消时保留已有文件；目标原本不存在时不留下不完整的目标文件。正常失败路径会清理临时文件，清理被文件系统拒绝时可能残留 `.ftp-*.tmp`；进程被强制终止时也可能残留。

- FtpDownloadAsync 的 CancellationToken 同时用于中断网络请求及数据复制，取消通过 OperationCanceledException 返回。下载成功仍覆盖同名文件，空文件下载成功也会正常替换。

- 邮件发送在成功、发送失败和消息构建失败时均释放客户端、消息、附件及正文视图。公开 GetSmtpClient(settings) 成功返回的客户端由调用方 using/Dispose；配置客户端失败由库内部释放。示例及资源回归测试通过本地投递目录验证，不向外部收件人发送邮件。

- FTP 的上传、删除、目录列表、建目录、大小查询、重命名及下载均按作用域释放文件流、响应和读取器，协议错误附带的响应也会关闭。上传会读取最终响应，服务端最终确认失败时不会误报成功；空目录明细返回空数组。资源回归使用回环地址测试服务器，未连接真实FTP服务。

- 默认示例只解析服务，不发送邮件、不连接 FTP。异步 FTP 上传下载需传 CancellationToken。FTP 使用传统协议，账号和网络边界由使用方配置。

- GetFtpListDirectoryDetails、FtpDelete、FtpReName、FtpMakeDirectory 等会实际访问远程服务。先准备专用测试目录，再显式调用；不要直接把示例当作生产操作脚本。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
