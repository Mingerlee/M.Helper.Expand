# M.Simple.Services.Email 接入与使用

独立 SMTP 邮件服务包。主机、端口和发件人替换为实际配置，密码通过配置或 User Secrets 提供。属性名 StmpHostServer 沿用现有 API 拼写。

当前项目版本 `1.0.0.1`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Services.Email --version 1.0.0.1
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## appsettings.json 配置

在 API 项目的 `appsettings.json` 中合并以下示例，替换为实际环境值：

```json
{
  "Email": {
    "Password": "your_smtp_password"
  }
}
```

对应 builder.Configuration["Email:Password"]。SMTP 主机、端口、发件人等仍按下方代码配置；本包不会自动绑定名为 Email 的配置节。

真实凭据可通过 User Secrets 或环境变量覆盖，不必写入配置文件；环境变量将键中的冒号替换为双下划线，例如 `Email__Password`。

## Program.cs 配置

```csharp
using M.Simple.Services.Email; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：配置并注册邮件服务。
builder.Services.AddSimpleEmail(options =>
{
    options.StmpHostServer = "smtp.example.com";
    options.PortNumber = 587;
    options.FromAddress = "sender@example.com";
    options.Username = "sender@example.com";
    options.Password = builder.Configuration["Email:Password"]
        ?? throw new InvalidOperationException("Email:Password is not configured.");
    options.RequireAuth = true;
    options.EnableSSL = true;
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

通过构造函数注入 IEMailService，按业务需要调用；下面的 mailService 为已注入服务。

```csharp
var result = await mailService.SendMailAsync(new MailContent
{
    ToAddresses = "recipient@example.com",
    Subject = "业务通知",
    Body = "通知内容",
    IsHTML = false
});
```

支持附件 AttachmentPaths、抄送、密送和回复地址，结果为 MailResult。



## 接入注意事项

服务与配置均为 Transient；每次发送创建并释放 SMTP 客户端、消息和附件。注册不会发送邮件，也不自动重试。公开 GetSmtpClient 返回的客户端由调用方释放。

## 构造函数注入与发送结果处理

```csharp
using M.Simple.Services.Email;

public sealed class NotificationService(IEMailService mailService)
{
    public async Task SendAsync(string recipient, string content)
    {
        var result = await mailService.SendMailAsync(new MailContent
        {
            ToAddresses = recipient,
            Subject = "业务通知",
            Body = content,
            IsHTML = false
        });
        if (result.Status == MailStatus.Failure)
            throw new InvalidOperationException(result.ErrorMsg);
    }
}
```

Build() 前注册 AddScoped<NotificationService>()，现有业务类注入后按需调用 SendAsync。检查 MailResult 的失败状态，同时保留调用可能抛出的异常交给现有异常处理。发送会实际访问 SMTP，注册服务本身不会发送。
