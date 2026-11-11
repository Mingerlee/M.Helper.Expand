# M.Simple.Expand 使用手册

本手册针对 D:/oym/M.Helper.Expand/M.Simple.Expand 当前原版源码，包版本 1.0.1.8，目标框架 .NET 8。它与另一目录的 M.Simple.Extensions 是独立项目，命名空间和 API 不互换。

## 安装

在调用方项目中安装匹配的 NuGet 版本：

```powershell
dotnet add package M.Simple.Expand --version 1.0.1.8
```

该命令要求对应包源实际存在此版本；本次未向公共 NuGet 发布。需要使用本地源码构建时，在本目录运行 dotnet build M.Simple.Expand.csproj --configuration Release，包生成于 bin/Release。可将该目录作为本地 NuGet 源。

扩展方法一般导入 using M.Simple.Expand；模型在 M.Simple.Expand.Models，特性在 M.Simple.Expand.Attributes，服务在 M.Simple.Expand.Services。部分历史加解密辅助类命名空间为 M.Simple.Helper。

## 功能分类

| 功能 | 说明 | 示例参数 |
|---|---|---|
| [字符串](usage/strings.md) | 遮盖、命名转换、全半角、匹配 | strings |
| [中文与拼音](usage/chinese.md) | 拼音、首字母、中文日期与农历 | chinese |
| [日期与时间](usage/datetime.md) | 格式、月份边界、日期差与区间 | datetime |
| [金额](usage/money.md) | 金额大写、舍入、格式化 | money |
| [集合与比较器](usage/collections.md) | 批次、排除、分组与按属性去重 | collections |
| [字典](usage/dictionary.md) | 覆盖合并、忽略合并、弹出与删除 | dictionary |
| [枚举](usage/enums.md) | 描述、Display、解析与下拉选项 | enums |
| [DataTable](usage/datatable.md) | 列表转表、表转模型与筛选 | datatable |
| [实体比较与赋值](usage/comparison.md) | 新旧字段差异、特性筛选与属性赋值 | comparison |
| [加解密与摘要](usage/crypto.md) | AES、RSA 加解密及签名 | crypto |
| [配置读取](usage/configuration.md) | 配置基准路径与 AppSetting | configuration |
| [依赖注入与属性装配](usage/di.md) | AppService 扫描和 Autowired | di |
| [邮件与 FTP](usage/services.md) | 配置、服务注册与实际调用入口 | services |
| [GUID 与编码](usage/identifiers.md) | GUID 格式、雪花 ID 与短码 | identifiers |
| [校验特性](usage/validation.md) | 手机号、邮箱与 DataAnnotations | validation |

## 一次运行全部示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- all
```

15 个场景覆盖常用能力并包含断言。邮件与 FTP 只验证服务注册，不进行外部通信。配置示例只读取随项目提供的演示 JSON。[示例说明](samples/README.md)、[实际运行输出](sample-output.md)、[API 参考](api.md)。

## 依赖与适用范围

库引用 Configuration 系列 10.0.3、DI Abstractions 9.0.2、PinYinConverterCore 1.0.2；示例额外引用 DI 容器实现 9.0.2。本项目不是零依赖基础包。

本手册依据当前源码和可运行示例编写。旧 XML 注释与实现不一致的地方，以各功能页中已核对的行为说明为准。未修改原功能实现，未验证真实 SMTP/FTP、跨机器 ID 唯一性、NativeAOT 或裁剪。

## 本地文档站点

访问 http://127.0.0.1:8091/。站点是静态 HTML，不需运行 .NET Web 服务。见[构建与 IIS 发布说明](publishing.md)。
