# M.Simple.Container Web 示例

```powershell
dotnet run --project M.Simple.Container.Sample -c Release -- --urls http://127.0.0.1:5198
```

- `GET /users/1`：演示业务层依赖仓储接口。
- `GET /scope`：`sharedInstance` 为 `true`，重复请求的 `scopeId` 不同。
- `GET /proxy?message=hello`：返回 `hello【代理前置】,hello world!【代理后置】`，`proxyType` 是自动生成的接口代理类型；重复请求的 `scopeId` 不同。

入口只需 `UseSimpleContainer()` 和 `AddAssemblyServices<IScopedService>(...)`；接口继承标记即可参与批量注入。

代理示例使用 Container 1.0.0.3 的无参数拦截扫描。应用示例同时引用 Container 和 Interception，两个库项目之间不增加引用：

```csharp
builder.Services.Scan(typeof(GreetingService).Assembly)
    .AssignableTo<IInterception>()
    .ExcludeInterface(typeof(IInterception))
    .AsImplementedInterfaces()
    .InterceptedScoped();
```

`IInterception` 由应用定义，仅筛选实现类，注册时排除该标记接口。`GreetingService` 使用 Interception 的 `BeforeMethod` / `AfterMethod` 特性，无需手写代理或工厂。Container 在注册阶段加载 Interception 的公开注册方法，之后正常调用接口代理；业务调用不经过适配反射。

同样支持无参数 `InterceptedTransient()`、`InterceptedSingleton()`。应用运行时必须提供 Interception 1.0.0.4 或兼容的后续版本；缺少运行时或 API 不兼容时，注册会明确报错。本项目通过 ProjectReference 引用对应源码。
