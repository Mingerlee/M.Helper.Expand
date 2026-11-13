# 邮件与 FTP

配置、服务注册与实际调用入口。适用独立包 M.Simple.Services.Email / M.Simple.Services.Ftp 1.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- services
```

```csharp
using M.Simple.Expand;
using M.Simple.Services.Email;
using M.Simple.Services.Ftp;
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
        var first = provider.GetRequiredService<IFtpClientService>();
        var second = provider.GetRequiredService<IFtpClientService>();
        first.GotoFtpDirectory("first", true);
        second.GotoFtpDirectory("second", true);
        Check(!ReferenceEquals(first, second), "FTP 每次解析独立实例");
        Check(((FtpClientService)first).Settings.FtpRemotePath == "first", "FTP 目录隔离");
        Console.WriteLine("邮件与 FTP 服务解析成功；仅演示注册，不建立网络连接。");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- AddEMailSetup 注册的 MailSettings 和 IEMailService 均为 Transient。AddFtpClientSetup 将 FtpClientSettings 和 IFtpClientService 均注册为 Transient：每次解析创建独立服务，同一个 scope 内再次解析也得到新实例。每个FTP实例复制配置模板，切换目录不会回写模板或其他实例。

- 每条独立的“切目录→操作文件”流程应单独解析 IFtpClientService；同一流程内复用该实例。不要在单例消费者中长期捕获一个FTP实例供多个并行流程共享。实例内加锁仅保证目录更新和单次配置快照一致，不保证共享同一实例时跨多次方法调用的流程原子性。

- FtpClientService.Settings 返回配置副本，直接修改该副本不再改变服务；需要应用修改时调用 ResetFtpClientSettings(settings)。构造和重置都会复制传入配置，之后修改原配置对象不影响服务。调用方不要在传入配置的同时并发修改该对象。每个请求的地址与凭据来自同一快照，重置影响后续构造的请求。

- 邮件主机字段名称为 StmpHostServer（历史拼写）；另有 FromAddress、PortNumber、EnableSSL、RequireAuth、Username、Password、DeliveryOption。

- 邮件内容使用 MailContent：ToAddresses、Subject、Body、IsHTML、AttachmentPaths。SendMail 和 SendMailAsync 返回 MailResult，状态字段见 API 参考。

- 邮件地址分别设置 ToAddresses（收件人）、CcAddresses（抄送）、BccAddresses（密送）、ReplyToAddresses（回复地址），各列表独立读取，支持逗号或分号分隔多个地址。可选地址为 null 或空字符串时不添加对应地址；回归测试只检查内存中的地址转换，不发送邮件。

- FTP 的 FtpServerIP 传主机或主机:端口，不带 ftp://；FtpRemotePath 为目录，FtpUrl 属性负责拼接地址。常用方法为 FtpUpload、FtpDownload、GetFtpFileList、GotoFtpDirectory。

- FtpClient.Download、FtpClientService.FtpDownload 和 FtpDownloadAsync 先写入目标目录的临时文件，完整下载并成功关闭流后才替换目标文件。连接失败、远端拒绝、传输失败或异步取消时保留已有文件；目标原本不存在时不留下不完整的目标文件。正常失败路径会清理临时文件，清理被文件系统拒绝时可能残留 `.ftp-*.tmp`；进程被强制终止时也可能残留。

- FtpUploadAsync、FtpDownloadAsync、FtpDeleteAsync、FtpRemoveDirectoryAsync 的网络等待均使用异步接口；上传异步获取请求流并等待最终确认，文件数据使用异步文件句柄复制。已取消的 token 会在访问文件或建立请求前直接终止；执行中取消通过 Abort 中断请求，覆盖等待服务器欢迎、命令响应、数据传输及最终确认等阶段。

- 取消通过 OperationCanceledException（可能为其子类 TaskCanceledException）传播，保留调用方 token，返回任务处于 Canceled 状态；不会包装成普通 Exception。非取消的协议错误仍作为失败抛出，并在 InnerException 中保留原异常。下载取消时保留旧文件并清理临时文件；正常空文件下载仍可替换原文件。

- 取消不会回滚服务器已执行的上传、删除或删目录操作；上传中断可能留下远端不完整文件，取消发生在最终响应前也不能据此判断服务器是否已完成操作。需要时由调用方核对远端结果。

- 邮件发送在成功、发送失败和消息构建失败时均释放客户端、消息、附件及正文视图。公开 GetSmtpClient(settings) 成功返回的客户端由调用方 using/Dispose；配置客户端失败由库内部释放。示例及资源回归测试通过本地投递目录验证，不向外部收件人发送邮件。

- FTP 的上传、删除、目录列表、建目录、大小查询、重命名及下载均按作用域释放文件流、响应和读取器，协议错误附带的响应也会关闭。上传会读取最终响应，服务端最终确认失败时不会误报成功；空目录明细返回空数组。资源回归使用回环地址测试服务器，未连接真实FTP服务。

- 默认示例只解析服务，不发送邮件、不连接 FTP。异步 FTP 上传下载需传 CancellationToken。FTP 使用传统协议，账号和网络边界由使用方配置。

- GetFtpListDirectoryDetails、FtpDelete、FtpReName、FtpMakeDirectory 等会实际访问远程服务。先准备专用测试目录，再显式调用；不要直接把示例当作生产操作脚本。

## API 查询

完整说明见独立包文档：[Email](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Services.Email/README.md)、[FTP](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Services.Ftp/README.md)。
