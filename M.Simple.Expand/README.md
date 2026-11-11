# M.Simple.Expand

.NET 8 通用扩展与服务包，覆盖字符串、日期、集合、模型转换、校验、配置、依赖注入、邮件和 FTP。当前源码包版本为 `1.0.1.8`。

扩展方法导入 `M.Simple.Expand`，服务导入 `M.Simple.Expand.Services`，模型、枚举和特性分别位于 `Models`、`Enums`、`Attributes` 命名空间。以下行为以当前仓库源码为准，使用已发布 NuGet 包时请核对对应版本。

## 安装

```powershell
dotnet add package M.Simple.Expand --version 1.0.1.8
```

安装命令要求所用包源中已发布该版本。自行构建时，在本目录执行：

```powershell
dotnet pack M.Simple.Expand.csproj --configuration Release
```

产物位于 `bin/Release/M.Simple.Expand.1.0.1.8.nupkg`，可将 `bin/Release` 配置为本地 NuGet 源；同一解决方案内也可直接引用本项目。库依赖 Configuration、DI Abstractions 和 PinYinConverterCore，具体版本以项目文件为准。使用默认 DI 容器的宿主还需引用 `Microsoft.Extensions.DependencyInjection`。

## 快速使用

```csharp
using M.Simple.Expand;

var phone = "13812345678".ToCover();
var amount = 123.45m.ToChinese();

// 保留来源顺序及重复项，排除2和4。
var remaining = new[] { 1, 2, 2, 3, 4 }.ExceptItems(new[] { 2, 4 }).ToArray(); // 1,3

Dictionary<string, int>? values = null;
values = values.Merges(new Dictionary<string, int> { ["A"] = 1, ["B"] = 2 });
```

## 功能分类

| 分类 | 常用能力 |
| --- | --- |
| 字符串、中文与金额 | 遮盖、命名转换、拼音、金额大写及格式化 |
| 日期与时间 | 日期格式、月份边界、日期差、区间重叠 |
| 集合、比较器与字典 | 分批、排除、分组、属性比较、覆盖或忽略合并 |
| 实体与 DataTable | 差异比较、属性赋值、列表与表的往返转换 |
| 枚举与校验 | 描述读取、选项转换、DataAnnotations 范围及格式校验 |
| 配置与依赖注入 | 配置读取、服务扫描、手动属性与字段装配 |
| 邮件、FTP 与编码 | 邮件发送、文件传输、GUID、短码、加解密及摘要 |

每类 API 的示例及边界见下方完整使用手册。

## 日期计算约定

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Enums;

var start = new DateTime(2026, 1, 31);
var end = new DateTime(2026, 2, 28);
var days = start.DiffDays(end);                          // 28
var months = start.DiffMonth(end, DiffMonthWayEnum.精确); // 1
var reverse = end.DiffMonth(start, DiffMonthWayEnum.精确);// -1
```

- `DiffDays` 默认方向与 `DiffMonth` 一致：结束日期较晚为正，较早为负；忽略时分秒，同日为0。`DiffDays(includeToday: true)` 沿用日期差直接加1的规则。
- “简单”和“跨月”计算自然月边界数，同月为0；1月31日到2月1日为1。
- “精确”以较早日期为锚点，按 `AddMonths(n)` 判断完整月数，目标月份无对应日时取月末；1月31日到非闰年2月28日为1，到闰年2月28日为0。交换起止日期结果取反。
- 当前实现修正了旧版 `DiffMonth` 的相反方向及边界问题，依赖旧符号或额外加1的调用方需要调整。

## 服务注册与并发使用

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFtpClientSetup(_ => new FtpClientSettings
{
    FtpServerIP = "ftp.example.invalid",
    FtpRemotePath = "reports"
});
using var provider = services.BuildServiceProvider();
var ftp = provider.GetRequiredService<IFtpClientService>();
ftp.GotoFtpDirectory("daily", false); // 本实例目录：reports/daily
// 配好实际FTP地址及认证后，再调用下载等网络方法。
```

- FTP 配置模板为 Singleton，服务为 **Transient**，每次解析得到独立实例。每条独立的“切目录→操作文件”流程单独解析一次；同一流程内复用该实例，避免将其长期捕获到单例中供并行流程共享。
- 配置在构造、重置时复制，每个请求使用一致快照。`FtpClientService.Settings` 返回副本，修改后需调用 `ResetFtpClientSettings` 应用。共享同一实例的多次方法调用不保证流程原子性。
- `AutowiredService` 保持 Singleton，并发缓存只保存装配委托。装配 scoped 依赖时显式传入 `Autowired(target, scope.ServiceProvider)`；单参数重载不会自动识别当前请求 scope。
- 邮件配置及服务保持 Singleton。发送方法负责释放客户端、消息及附件；通过公开 `GetSmtpClient(settings)` 自行创建的客户端由调用方释放。

## 转换与结果边界

- `ExceptItem/ExceptItems` 默认按实体公共可读属性比较，值类型和字符串使用自身相等规则；可传自定义比较器。
- `CompareModel` 比较真实属性值，区分 `null` 和空串，包含 `object` 属性；新实体为 `null` 时报告删除。引用属性使用自身 `Equals`，不递归比较对象图或集合内容。
- `SetStringValue` 支持基础数值、可空类型、枚举、Guid 等转换；非法或溢出输入抛出异常并保留原值。可空值类型的空白文本表示清空。
- DataTable 转换保留显示列名及原属性映射，支持可空值和枚举往返。泛型空列表保留可推断的表结构；`ToLists(null)` 返回空列表，`Where` 的空源返回空表，空筛选条件报错。
- 范围校验解析失败直接返回错误，不再将非法文本当作0；`ConfigExist` 能区分缺失节点和存在的父节点。
- 字典批量合并支持 `null` 目标，调用方必须接收返回值。空区间列表的 `IsOverlap` 返回 false。
- FTP 下载先写同目录临时文件，成功后才替换目标；失败或取消时保留旧文件并尝试清理临时文件。进程强制退出或清理被拒绝时可能残留临时文件。
- 短码唯一性以当前进程内复用生成器为边界；跨进程、跨机器的唯一性需要额外协调。

## 使用手册与示例

- [完整使用手册](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Expand/docs/usage.md)：15 类功能及当前行为边界。
- [可运行示例](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Expand/docs/samples/README.md)：包含断言的 .NET 8 控制台示例。
- [API 与源码索引](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Expand/docs/api.md)：从实际构建程序集及 XML 文档生成。
- [IIS 发布说明](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Expand/docs/publishing.md)：生成站点、挂载和更新。
- 本地 Web 手册：[http://127.0.0.1:8091/](http://127.0.0.1:8091/)。

本项目是原版 `M.Simple.Expand`，与独立重建的 `M.Simple.Extensions` 不互换 API。

## 验证与更新手册

在本目录执行：

```powershell
dotnet test ../M.Simple.Expand.Test/M.Simple.Expand.Test.csproj --configuration Release
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- all
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-DocsSite.ps1
```

站点输出到 `website`，由本地 IIS 的8091端口提供访问；首次挂载步骤见发布说明。运行全部示例会执行15个带断言的场景，邮件与 FTP 示例不进行外部通信。测试另使用本地投递目录、回环 FTP 测试端及并发压力场景，不代表已验证真实服务器、NativeAOT 或裁剪兼容性。
