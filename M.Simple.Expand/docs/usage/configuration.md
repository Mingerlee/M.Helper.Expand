# 配置读取

配置基准路径与 AppSetting。适用 M.Simple.Expand 3.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- configuration
```

```csharp
using M.Simple.Expand;
using Microsoft.Extensions.Configuration;

static partial class Demo
{
    public static void Configuration()
    {
        var config = new ConfigurationBuilder()
            .SetConfigurationBasePath(AppContext.BaseDirectory)
            .AddJsonFileWithBasePath("sample-settings.json", optional: false)
            .Build();
        Console.WriteLine($"配置：{config["Demo:Name"]}");
        Check(config["Demo:Name"] == "M.Simple.Expand", "基础路径配置");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- SetConfigurationBasePath 记录基础路径；AddJsonFileWithBasePath 按路径组合后调用标准 AddJsonFile。示例将 sample-settings.json 复制到输出目录并从 AppContext.BaseDirectory 读取。

- AppSetting 静态入口首次访问时从 Directory.GetCurrentDirectory() 读取必需的 appsettings.json 并启用 reloadOnChange；当前工作目录和程序目录不一定相同。

- AppSetting 提供 GetConfig、GetConfigInt32、GetConfigBoolean 和 GetConfigSection<T>。缺值和转换异常请按方法实现处理，不假设均有默认值。

- ConfigExist 使用 IConfiguration.GetSection(...).Exists() 判断节点存在性：缺失节点返回 false，有值节点（包括空串）或包含子节点的父节点返回 true；读取配置发生异常仍返回 false。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
