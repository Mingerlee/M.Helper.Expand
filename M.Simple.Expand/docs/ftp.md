# M.Simple.Services.Ftp 接入与使用

独立 FTP 文件服务包，保留 FtpWebRequest 实现，不提供 SFTP。主机、账号、远端目录替换为实际值，密码通过配置提供。

当前项目版本 `1.0.0.1`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Services.Ftp --version 1.0.0.1
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## appsettings.json 配置

在 API 项目的 `appsettings.json` 中合并以下示例，替换为实际环境值：

```json
{
  "Ftp": {
    "Password": "your_ftp_password"
  }
}
```

对应 builder.Configuration["Ftp:Password"]。FTP 主机、账号、远端目录仍按下方代码配置；本包不会自动绑定名为 Ftp 的配置节。

真实凭据可通过 User Secrets 或环境变量覆盖，不必写入配置文件；环境变量将键中的冒号替换为双下划线，例如 `Ftp__Password`。

## Program.cs 配置

```csharp
using M.Simple.Services.Ftp; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：配置并注册 FTP 服务。
builder.Services.AddSimpleFtp(options =>
{
    options.FtpServerIP = "ftp.example.com";
    options.FtpUserID = "your_user";
    options.FtpPassword = builder.Configuration["Ftp:Password"]
        ?? throw new InvalidOperationException("Ftp:Password is not configured.");
    options.FtpRemotePath = "uploads";
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

通过构造函数注入 IFtpClientService。下面 ftp 为已注入服务，token 为当前取消令牌：

```csharp
bool uploaded = await ftp.FtpUploadAsync(@"D:\files\report.xlsx", token);
bool downloaded = await ftp.FtpDownloadAsync(@"D:\downloads", "report.xlsx", token);
```

上传和下载为独立调用示例，按业务选择；也提供目录查询、创建、删除及文件重命名等操作。



## 接入注意事项

服务和配置均为 Transient，实例持有配置副本和当前目录。并行多步目录流程应分别解析实例。取消不能撤销远端已执行操作，中断上传可能留下部分文件。注册不会连接外部 FTP。

## 构造函数注入与上传调用

```csharp
using M.Simple.Services.Ftp;

public sealed class FileTransferService(IFtpClientService ftp)
{
    public async Task UploadAsync(string localPath, CancellationToken token)
    {
        bool uploaded = await ftp.FtpUploadAsync(localPath, token);
        if (!uploaded)
            throw new InvalidOperationException("FTP 上传未成功。");
    }
}
```

Build() 前注册 AddTransient<FileTransferService>()，现有业务类注入后调用 UploadAsync，并传入实际本地文件绝对路径及取消令牌。异常和取消向调用方传播；业务类不自行创建 FTP 连接。远端目录由注册时的 FtpRemotePath 决定。
