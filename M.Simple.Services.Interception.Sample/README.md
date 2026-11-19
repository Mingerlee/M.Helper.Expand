# AOP 代理示例

在仓库根目录运行：

```powershell
dotnet run --project M.Simple.Services.Interception.Sample -c Release
```

示例使用本地项目引用，不依赖已发布的新包，且不连接数据库、Redis 或外部服务。

演示同步返回值、Task、ValueTask、方法特性筛选、跳过、异常后置，以及默认接口整体拦截。还包含新增的 BeforeMethod(nameof(...)) / AfterMethod(nameof(...))：直接指定同一服务的私有方法，无需独立拦截器，并验证成功与失败时的执行顺序。执行结果带断言，最后输出“全部示例验证通过”。

业务方法仍通过接口正常调用，DI 注册使用 `AddInterceptedScoped<IOrderService, OrderService, ConsoleMethodInterceptor>(attributedOnly: true)`。

按方法名特性的示例使用 `AddInterceptedScoped<INamedOrderService, NamedOrderService>()`。该能力从 NuGet 包 1.0.0.2 起提供。

完整规则见[包 README](../M.Simple.Services.Interception/README.md)。
