# M.Simple.Expand

.NET 8 通用扩展与服务包，当前源码版本为 `1.0.1.8`。

## 安装

```powershell
dotnet add package M.Simple.Expand --version 1.0.1.8
```

安装命令要求所用包源中存在该版本。本地构建包位于 `bin/Release`，也可将其作为本地包源使用。

```csharp
using M.Simple.Expand;

var phone = "13812345678".ToCover();
var amount = 123.45m.ToChinese();
```

## 使用手册与示例

- [完整使用手册](docs/usage.md)：15 类功能及原版行为边界。
- [可运行示例](docs/samples/README.md)：包含断言的 .NET 8 控制台示例。
- [API 与源码索引](docs/api.md)：从实际构建程序集及 XML 文档生成。
- [IIS 发布说明](docs/publishing.md)：生成站点、挂载和更新。
- 本地 Web 手册：[http://127.0.0.1:8091/](http://127.0.0.1:8091/)。

本项目是原版 `M.Simple.Expand`，与独立重建的 `M.Simple.Extensions` 不互换 API。

## 运行与更新手册

在本目录执行：

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- all
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-DocsSite.ps1
```

邮件与 FTP 示例只验证注册和解析，不执行网络通信。
