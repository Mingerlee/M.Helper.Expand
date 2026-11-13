# 依赖注入与属性装配

AppService 扫描和 Autowired。适用独立包 M.Simple.Services.DependencyInjection 1.0.0-preview.1 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- di
```

```csharp
using M.Simple.Expand;
using M.Simple.Services.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

static partial class Demo
{
    public static void DependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddAppServices(typeof(SampleGreeting).Assembly);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var consumer = new SampleConsumer();
        scope.ServiceProvider.GetRequiredService<AutowiredService>().Autowired(consumer);
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

- 导入 M.Simple.Services.DependencyInjection 和 Microsoft.Extensions.DependencyInjection。宿主需提供 DI 容器实现；库主要引用 DI Abstractions，示例额外安装容器包。

- AddAppServices 推荐显式传入程序集；无参数时扫描当前 AppDomain 已加载程序集里的 AppServiceAttribute。默认生命周期为 Singleton；未指定 ServiceType 时尝试取第一个接口，否则按自身类型注册。多接口类型建议显式填写 ServiceType。

- AutowiredAttribute 只是标记，不会让默认容器自动完成属性注入。示例显式解析 AutowiredService 并调用 Autowired(consumer)。

- AutowiredService 改为 Scoped，使用 ConcurrentDictionary 与 Lazy 缓存装配委托，同一装配服务实例内同类型只编译一次。委托不缓存依赖实例，每次装配都从本次传入的 IServiceProvider 解析服务；可并发装配不同目标对象。

- Scoped 服务应从对应 scope 解析，调用 Autowired(target, scope.ServiceProvider)；单参数重载仍使用创建 AutowiredService 时的 provider，不会自动识别当前请求作用域。不要让单例捕获 scoped 对象，也不要并发装配同一个有可变属性的目标对象。程序集扫描失败及动态程序集类型加载异常仍按原行为处理。

## API 查询

[独立包说明](https://github.com/Mingerlee/M.Helper.Expand/blob/main/M.Simple.Services.DependencyInjection/README.md)提供注册与装配约定。
