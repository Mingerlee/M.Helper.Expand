# M.Simple.Services.DependencyInjection

.NET 8 独立特性注册与手动装配包，版本 `1.0.0-preview.1`，不依赖 `M.Simple.Expand` 或其他服务包。

```csharp
using M.Simple.Services.DependencyInjection;

services.AddAppServices(typeof(OrderService).Assembly);
// 只需要手动装配时：services.AddSimpleDependencyInjection();
```

`AppServiceAttribute` 支持指定 `ServiceType` 和 `Lifetime`。保留原特性默认 Singleton 的语义，业务服务有作用域依赖时应显式选择 Scoped。`AutowiredService` 自身改为 Scoped，应从当前作用域解析。

`AddAppServices` 推荐显式指定程序集，无参数时扫描当前已加载程序集；跳过抽象类和开放泛型，验证服务类型并避免同实现重复注册。程序集加载失败会直接报错。实现多个接口时建议显式指定 ServiceType。

`[Autowired]` 不会自动触发微软 DI 属性注入，需要主动装配：

```csharp
using var scope = provider.CreateScope();
var consumer = new Consumer();
scope.ServiceProvider.GetRequiredService<AutowiredService>().Autowired(consumer);
```

缓存只保存装配委托，不缓存依赖实例。装配使用反射和表达式编译，首版不承诺 NativeAOT / trimming 支持。

测试位于根目录 `M.Simple.Services.DependencyInjection.Test`。示例位于 `M.Simple.Expand/docs/samples/DependencyInjection.cs.txt`。

```powershell
dotnet test M.Simple.Services.DependencyInjection.Test/M.Simple.Services.DependencyInjection.Test.csproj -c Release
dotnet pack M.Simple.Services.DependencyInjection/M.Simple.Services.DependencyInjection.csproj -c Release -o artifacts/packages
```

原服务及注入特性统一改用 `M.Simple.Services.DependencyInjection` 命名空间。新包仅本地打包，尚未自动发布到 NuGet。
