# M.Simple.Services.DependencyInjection 接入与使用

独立特性注册与手动属性装配包。OrderService 替换为项目实际业务类型；不同于 M.Simple.Container，它不会替换 Host 的容器。

当前项目版本 `1.0.0.1`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.Services.DependencyInjection --version 1.0.0.1
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## Program.cs 配置

```csharp
using M.Simple.Services.DependencyInjection; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAppServices(typeof(OrderService).Assembly); // 本次新增：扫描指定程序集中的 AppService 特性并注册服务。

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

业务类使用 AppService 指定接口及生命周期：

```csharp
using M.Simple.Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

public interface IOrderService { }

[AppService(ServiceType = typeof(IOrderService), Lifetime = ServiceLifetime.Scoped)]
public sealed class OrderService : IOrderService { }
```

属性装配需要显式调用，Autowired 特性本身不会触发容器自动注入：

```csharp
using var scope = provider.CreateScope();
var consumer = new Consumer();
scope.ServiceProvider.GetRequiredService<AutowiredService>().Autowired(consumer);
```

provider 为服务提供程序，Consumer 为项目中带 Autowired 属性的类型。只需手动装配时，可用 AddSimpleDependencyInjection() 替代扫描注册。



## 接入注意事项

AppService 默认 Singleton，有 Scoped 依赖时显式选择 Scoped。AutowiredService 为 Scoped，需从相应作用域解析。多接口实现建议明确 ServiceType。无参数扫描只覆盖当前已加载程序集；推荐显式传入程序集。
