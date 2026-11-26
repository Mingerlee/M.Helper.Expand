# AOP 代理示例

在仓库根目录运行：

```powershell
dotnet run --project M.Simple.Services.Interception.Sample -c Release
```

示例使用本地项目引用，不依赖已发布的新包，且不连接数据库、Redis 或外部服务。

演示同步返回值、Task、ValueTask、方法特性筛选、跳过、业务失败跳过后置，以及默认接口整体拦截。还包含新增的 BeforeMethod(nameof(...)) / AfterMethod(nameof(...))：直接指定同一服务的私有方法，无需独立拦截器，并验证成功与失败时的执行顺序。执行结果带断言，最后输出“全部示例验证通过”。

业务方法仍通过接口正常调用，DI 注册使用 `AddInterceptedScoped<IOrderService, OrderService, ConsoleMethodInterceptor>(attributedOnly: true)`。

按方法名特性的示例使用 `AddInterceptedScoped<INamedOrderService, NamedOrderService>()`。该能力从 NuGet 包 1.0.0.2 起提供。

完整规则见[包 README](../M.Simple.Services.Interception/README.md)。

新增第 9 个场景：前置使用 GetArgument/SetArgument 拼接 msg，后置使用 SetResult 追加返回值，断言最终为“你好【前置追加】【后置追加】”。参数和结果替换从 NuGet 包 1.0.0.3 起提供。

当前源码第 9 个场景使用外部 MessageHooks 演示 DI 钩子注入。前置失败记录日志后继续，业务失败跳过后置，后置失败记录日志并保留结果。
