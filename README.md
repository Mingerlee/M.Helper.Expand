# M.Helper.Expand

新增独立日志包 **[M.Helper.Logging](M.Helper.Logging/README.md)**：.NET 8 标准日志接口、本地 SQLite 和内嵌 Dashboard。快速体验：`dotnet run --project M.Helper.Logging.Sample`，访问 <http://127.0.0.1:5188/logs>。

本仓库当前维护的通用扩展包为 **M.Simple.Expand**，目标框架为 .NET 8，供多个 .NET 项目复用，也可构建为 NuGet 包。

- [包说明与快速示例](M.Simple.Expand/README.md)
- [完整使用手册](M.Simple.Expand/docs/usage.md)
- [可运行示例](M.Simple.Expand/docs/samples/README.md)
- [API 与源码索引](M.Simple.Expand/docs/api.md)
- [本地 IIS 手册站点](http://127.0.0.1:8091/)（需先在本机生成并挂载）

## 构建与测试

在仓库根目录运行：

```powershell
dotnet build M.Simple.Expand/M.Simple.Expand.csproj --configuration Release
dotnet test M.Simple.Expand.Test/M.Simple.Expand.Test.csproj --configuration Release
```

安装、功能分类、日期计算约定、FTP 生命周期及文档站点更新步骤见[包 README](M.Simple.Expand/README.md)。
