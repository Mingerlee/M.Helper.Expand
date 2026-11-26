# M.Simple.Container Web 示例

```powershell
dotnet run --project M.Simple.Container.Sample -c Release -- --urls http://127.0.0.1:5198
```

- `GET /users/1`：演示业务层依赖仓储接口。
- `GET /scope`：`sharedInstance` 为 `true`，重复请求的 `scopeId` 不同。
- `GET /proxy?message=hello`：返回 `hello【代理前置】,hello world!【代理后置】`，`proxyType` 为 `GreetingServiceProxy`；重复请求的 `scopeId` 不同。

入口只需 `UseSimpleContainer()` 和 `AddAssemblyServices<IScopedService>(...)`；接口继承标记即可参与批量注入。

代理示例使用 Container 1.0.0.2 的指定接口和代理工厂注册，不引用 Interception：

```csharp
builder.Services.Scan(typeof(GreetingService).Assembly)
    .AssignableTo<IProxyService>()
    .Where(type => type == typeof(GreetingService))
    .As<IGreetingService>()
    .ProxyScoped((_, target, _) => new GreetingServiceProxy((IGreetingService)target));
```

`IProxyService` 只用于筛选，`As<IGreetingService>()` 只注册业务接口。`Where` 只选择真实业务类，避免同程序集中的手写代理也被当作业务实现扫描。`GreetingServiceProxy` 负责演示前置修改参数和后置修改结果；真实服务及代理的 Scoped 生命周期由 Container 管理。可替换为 `WithProxy(factory).Scoped()`，也支持 `ProxyTransient` 和 `ProxySingleton`。
