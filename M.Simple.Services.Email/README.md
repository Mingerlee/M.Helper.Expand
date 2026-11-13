# M.Simple.Services.Email

.NET 8 独立邮件服务包，版本 `1.0.0-preview.1`，不依赖 `M.Simple.Expand` 或其他服务包。

```csharp
using M.Simple.Services.Email;

services.AddSimpleEmail(options =>
{
    options.StmpHostServer = "smtp.example.com";
    options.PortNumber = 587;
    options.FromAddress = "sender@example.com";
    options.Username = "sender@example.com";
    options.Password = password;
    options.RequireAuth = true;
    options.EnableSSL = true;
});
```

注入 `IEMailService`，使用 `MailContent` 调用 `SendMail` / `SendMailAsync`，结果为 `MailResult`。保留附件、抄送、密送、回复地址及本地拾取目录。旧配置工厂入口 `AddEMailSetup(provider => settings)` 保留。

服务和配置均按 Transient 解析；配置工厂可使用当前作用域依赖，每次解析服务会调用工厂。直接构造服务时不要并发修改传入配置。每次发送创建并释放 SMTP 客户端、消息及附件。公开 `GetSmtpClient(settings)` 返回的客户端由调用方释放。首版沿用原有 `SmtpClient` 实现，不自动发送或重试。

测试位于根目录的 `M.Simple.Services.Email.Test`，只使用内存模型和本地拾取目录，不发送外部邮件。注册示例位于 `M.Simple.Expand/docs/samples/Services.cs.txt`。

```powershell
dotnet test M.Simple.Services.Email.Test/M.Simple.Services.Email.Test.csproj -c Release
dotnet pack M.Simple.Services.Email/M.Simple.Services.Email.csproj -c Release -o artifacts/packages
```

原 `M.Simple.Expand.Services` 改为 `M.Simple.Services.Email`。新包只做本地打包，不代表已发布到公共 NuGet。
