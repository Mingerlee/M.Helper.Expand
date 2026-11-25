# M.Helper.Expand 旧包使用说明

旧综合扩展包，当前项目版本 `1.0.3.5`，目标框架 .NET 8。它与 `M.Simple.Expand` 是不同的程序集及命名空间，不能直接互换。

## 安装包

```powershell
dotnet add package M.Helper.Expand --version 1.0.3.5
```

包源需包含对应版本。旧项目可保持原引用；新项目按所需功能选择当前 M.Simple 系列包。

## 使用方式

普通扩展方法无需在 Program.cs 注册服务，在业务文件中引入命名空间即可：

```csharp
using M.Helper.Expand; // 本次新增：旧综合扩展包的命名空间。

var masked = "example".ToCover();
```

项目还包含日期、集合、字典、枚举、DataTable、比较、加解密以及 Office 辅助类。不同辅助类的参数与返回约定以其源码为准，勿套用 M.Simple.Expand 的 API。

## 依赖与迁移

旧包包含 Aspose.Cells、iText、NPOI、SkiaSharp、Newtonsoft.Json 等依赖。Office 功能受相应组件的许可证、平台依赖和文件格式支持约束。

迁移至拆分包时，按功能选择：

- [M.Simple.Expand 通用扩展](usage.md)：字符串、集合、日期等。
- [邮件服务](email.md)与[FTP 服务](ftp.md)：独立服务注册及构造函数注入。
- [特性注册与属性装配](dependency-injection.md)：独立注入特性与装配入口。

迁移需要修改包引用、命名空间和相关调用，再验证原业务行为；本文更新不会自动迁移使用方代码。
