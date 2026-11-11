# 依赖注入与属性装配

AppService 扫描和 Autowired。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- di
```

```csharp
using M.Simple.Expand;
using M.Simple.Expand.Attributes;
using M.Simple.Expand.Services;
using Microsoft.Extensions.DependencyInjection;

static partial class Demo
{
    public static void DependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddAppServices();
        using var provider = services.BuildServiceProvider();
        var consumer = new SampleConsumer();
        provider.GetRequiredService<AutowiredService>().Autowired(consumer);
        Console.WriteLine(consumer.Greeting.Text);
        Check(consumer.Greeting.Text == "服务装配成功", "手动属性装配");
    }
}
public interface ISampleGreeting { string Text { get; } }
[AppService(ServiceType = typeof(ISampleGreeting), Lifetime = ServiceLifetime.Singleton)]
public class SampleGreeting : ISampleGreeting { public string Text => "服务装配成功"; }
public class SampleConsumer
{
    [Autowired] public ISampleGreeting Greeting { get; set; } = null!;
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- 导入 M.Simple.Expand.Attributes、M.Simple.Expand.Services 和 Microsoft.Extensions.DependencyInjection。宿主需提供 DI 容器实现；库主要引用 DI Abstractions，示例额外安装容器包。

- AddAppServices 扫描当前 AppDomain 已加载程序集里的 AppServiceAttribute。默认生命周期为 Singleton；未指定 ServiceType 时尝试取第一个接口，否则按自身类型注册。多接口类型建议显式填写 ServiceType。

- AutowiredAttribute 只是标记，不会让默认容器自动完成属性注入。示例显式解析 AutowiredService 并调用 Autowired(consumer)。

- Scoped 服务应从对应 scope 解析；不要让默认单例捕获 scoped 对象。程序集扫描失败、动态加载和并发缓存等边界未做额外封装。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
