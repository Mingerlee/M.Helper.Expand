# 可运行示例项目

要求 .NET 8 SDK。项目直接引用 ../../M.Simple.Expand.csproj，不依赖公共源上是否已发布库版本。首次构建需要还原项目所声明的依赖包。

## 运行

在 M.Simple.Expand 目录运行：

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- all
```

不传参数默认 all，也支持单个场景。未知参数返回退出码 1；断言失败直接报错。

| 参数 | 示例文件 | 场景 |
|---|---|---|
| strings | [Strings.cs.txt](Strings.cs.txt) | 字符串 |
| chinese | [Chinese.cs.txt](Chinese.cs.txt) | 中文与拼音 |
| datetime | [Dates.cs.txt](Dates.cs.txt) | 日期与时间 |
| money | [Money.cs.txt](Money.cs.txt) | 金额 |
| collections | [Collections.cs.txt](Collections.cs.txt) | 集合与比较器 |
| dictionary | [Dictionary.cs.txt](Dictionary.cs.txt) | 字典 |
| enums | [Enums.cs.txt](Enums.cs.txt) | 枚举 |
| datatable | [DataTable.cs.txt](DataTable.cs.txt) | DataTable |
| comparison | [Comparison.cs.txt](Comparison.cs.txt) | 实体比较与赋值 |
| crypto | [Crypto.cs.txt](Crypto.cs.txt) | 加解密与摘要 |
| configuration | [Configuration.cs.txt](Configuration.cs.txt) | 配置读取 |
| di | [DependencyInjection.cs.txt](DependencyInjection.cs.txt) | 依赖注入与属性装配 |
| services | [Services.cs.txt](Services.cs.txt) | 邮件与 FTP |
| identifiers | [Identifiers.cs.txt](Identifiers.cs.txt) | GUID 与编码 |
| validation | [Validation.cs.txt](Validation.cs.txt) | 校验特性 |

## 文件组织

示例源码采用 .cs.txt 扩展名，由示例 csproj 的 Compile Include 显式编译。父项目同时排除 docs、artifacts、website 下的生成 C# 文件，避免程序集特性重复。Program.cs.txt 包含场景入口和共享断言，DataTable.cs.txt 提供集合示例共用的 SampleRow 模型。

配置文件 sample-settings.json 复制到程序输出目录。示例不会覆盖工作目录下的 appsettings.json。运行时固定 zh-CN 文化和演示日期以使输出可读；ID 和密钥只校验，不直接输出。

邮件与 FTP 场景只实例化容器服务，不连接任何主机。完整调用规则见[使用手册](../usage.md)。
