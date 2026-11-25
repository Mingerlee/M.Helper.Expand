# M.Simple.Services.Email 接入与使用

独立 SMTP 邮件服务包。主机、端口和发件人替换为实际配置，密码通过配置或 User Secrets 提供。属性名 StmpHostServer 沿用现有 API 拼写。

当前项目版本 `1.0.0.1`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Services.Email --version 1.0.0.1
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

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
