# M.Simple.Services.Interception

.NET 8 接口 AOP 代理，当前源码版本 `1.0.0.2`。基于框架内置 DispatchProxy，只依赖 Microsoft.Extensions.DependencyInjection.Abstractions，不依赖 M.Simple.Expand 或其他服务包。支持方法前置、后置、特性筛选和 DI 生命周期。

1.0.0.2 新增 BeforeMethod / AfterMethod 与双泛型注册重载，可直接用方法名指定同一服务中的前置、后置方法。

## 快速体验

```powershell
dotnet run --project M.Simple.Services.Interception.Sample -c Release
dotnet test M.Simple.Services.Interception.Test -c Release
```

开发期间使用本地 ProjectReference。接口必须通过注册后的代理调用。源码、测试和[可运行示例](../M.Simple.Services.Interception.Sample/README.md)沿用根目录平铺结构。

## 注册和调用

### 直接指定前置、后置方法（1.0.0.2 起）

```csharp
using M.Simple.Services.Interception;

// 无需定义 MethodInterceptor；默认按特性启用。
builder.Services.AddInterceptedScoped<IOrderService, OrderService>();

public interface IOrderService
{
    Task<string> CreateAsync(string name);
}

public sealed class OrderService : IOrderService
{
    [BeforeMethod(nameof(BeforeCreate))]
    [AfterMethod(nameof(AfterCreate))]
    public async Task<string> CreateAsync(string name)
    {
        await Task.Delay(10);
        return name + ":created";
    }

    private void BeforeCreate(MethodInvocationContext context)
        => Console.WriteLine("参数：" + context.Arguments[0]);

    private Task AfterCreate(MethodInvocationContext context)
    {
        Console.WriteLine($"结果={context.Result}，异常={context.Exception?.Message}");
        return Task.CompletedTask;
    }
}
```

执行顺序：BeforeCreate → CreateAsync → AfterCreate。不必再加 MethodHook 特性；也可以只指定其中一个阶段。手动创建可调用 `MethodHookProxy<IOrderService>.Create(new OrderService())`。双泛型注册同样提供 Transient、Singleton 重载，默认 attributedOnly=true。

指定方法的约定：

- 默认查找同一真实服务实例的方法，支持 private、protected、public 及基类私有方法。方法名称区分大小写，建议用 nameof。按运行时类型向基类逐层查找，使用最靠近派生类型的同名声明层；不查其他服务或静态方法。
- 签名必须是单个 MethodInvocationContext 参数，返回 void、Task 或 ValueTask；不支持泛型钩子、Task<T>/ValueTask<T>、async void。同步业务方法只能配置 void 钩子，避免阻塞异步等待。
- 接口方法也可配置特性；同一阶段实现方法的特性覆盖接口方法的特性，两阶段分别判断。方法上的 SkipMethodHook 优先跳过全部钩子，该方法的名称配置也不做解析。
- 代理创建（DI 首次解析）时校验名字与签名，错误立即抛出；不等到调用业务后才发现。不允许业务方法把自己作为钩子。
- 同时使用通用拦截器时，顺序为“通用前置 → 指定前置 → 业务 → 指定后置 → 通用后置”。指定后置抛异常时仍尝试通用后置；两者都失败会合并为 AggregateException。业务也失败时仍保留原业务异常，后置失败附加在其 Data 中。
- 前置阶段任一步失败，停止业务且不运行后置；业务失败或取消时仍执行后置。钩子反射异常会解包为真实异常。
- 指定钩子直接调用真实对象，不再次进入接口代理；类内部自调用及直接调用真实服务仍不会触发特性。

### 使用通用拦截器

```csharp
using M.Simple.Services.Interception;

// 默认拦截接口全部方法，接口和实现类都不必加特性。
builder.Services.AddInterceptedScoped<IOrderService, OrderService, OrderInterceptor>();

// 或仅拦截带 MethodHook 的类、接口或方法（两种注册选择一种）：
// builder.Services.AddInterceptedScoped<IOrderService, OrderService, OrderInterceptor>(attributedOnly: true);

public interface IOrderService
{
    Task<string> CreateAsync(string name, CancellationToken cancellationToken = default);
}

public sealed class OrderService : IOrderService
{
    public async Task<string> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return name + ":created";
    }
}

public sealed class OrderInterceptor : MethodInterceptor
{
    public override void Before(MethodInvocationContext context)
    {
        Console.WriteLine("开始：" + context.Method.Name);
    }
    public override void After(MethodInvocationContext context)
    {
        Console.WriteLine(context.Exception is null
            ? $"完成：{context.Method.Name}，结果={context.Result}"
            : $"失败：{context.Method.Name}，异常={context.Exception.Message}");
    }
}
```

Controller 注入 `IOrderService` 并正常调用即可。同步钩子默认也用于异步方法；需要异步 I/O 时重写 `BeforeAsync` / `AfterAsync`，返回 ValueTask，并 await 自己的操作。同步业务方法只运行 `Before` / `After`，不会阻塞等待异步钩子。

同样提供 `AddInterceptedTransient`、`AddInterceptedSingleton`，泛型参数顺序一致。实现类和拦截器通过 DI 解析，构造函数可注入 ILogger、仓储等服务。已注册的实现类/拦截器会保留原注册，应确保其生命周期与代理匹配，特别是 Singleton 不能引用 Scoped 服务。

也可手动创建：

```csharp
var service = MethodHookProxy<IOrderService>.Create(new OrderService(), new OrderInterceptor());
await service.CreateAsync("demo");
```

## 类维度和方法维度

注册以接口/服务为单位，钩子按每次方法调用执行。可把特性放在接口、接口方法、实现类或实现方法上：

```csharp
[MethodHook] // attributedOnly=true 时，该接口整体启用钩子
public interface IInventoryService
{
    Task ReserveAsync();
    [SkipMethodHook] Task QueryAsync();
}

// 或在实现类上使用 [MethodHook]，在某个实现方法上使用 [SkipMethodHook]。
```

优先级从高到低：

1. 接口或实现方法有 SkipMethodHook：跳过。
2. 接口或实现方法有 MethodHook、BeforeMethod 或 AfterMethod：执行，可覆盖类型级跳过。
3. 注册接口（含继承接口）或实现类有 SkipMethodHook：跳过。
4. 默认模式执行；attributedOnly=true 时，类型上有 MethodHook 才执行。

接口和实现冲突时，同层级 Skip 优先。实现类上的特性按继承规则读取，支持显式接口实现；不通过方法名称猜测实现，按接口映射匹配。

## 上下文与异常

- `Method`：接口方法，泛型调用为实际构造方法；`ImplementationMethod`：映射到的实现方法。
- `Target`：真实服务对象；`Arguments`：只读参数列表（对象自身仍可变，第一版不提供参数或结果替换）。
- `Result`：业务返回值，Task<T>/ValueTask<T> 为等待后的 T，void/Task/ValueTask 为 null；失败时无有效结果。
- `Exception`：原始业务异常；`IsCanceled`：业务异常是否为 OperationCanceledException。
- `CancellationToken`：第一个 CancellationToken 参数；`Items`：本次调用前后共享的数据，可记录 Stopwatch 等。每次调用独立创建上下文。

前置成功后才调用业务。前置失败会停止业务，且不执行后置。业务成功、失败或取消均执行后置；异步后置会被等待，不能把取消后的 token 无条件用于必须完成的清理工作。

业务成功而后置失败：后置异常传给调用方。业务和后置同时失败：保留原业务异常及堆栈，后置异常尽力附加到其 `Data[MethodHookProxy<IOrderService>.AfterExceptionDataKey]`；Data 无法写入时仍保留业务异常。后置不自动记录日志，避免与宿主全局异常日志重复；业务是否回滚由业务层决定。

## 支持范围

- 同步 void/普通返回值、Task、Task<T>、ValueTask、ValueTask<T>；异步后置等待真实任务完成，不在返回任务对象时提前运行。
- 支持普通泛型方法、继承接口和显式接口实现。公开属性访问器也会经过代理；默认模式同样执行钩子。
- 一次注册对应一个拦截器，可在该拦截器内部组合多个逻辑。当前不提供多层拦截器管线、重试、短路返回或事务自动处理。
- 调用真实对象、new 服务类、类内部 this.OtherMethod() 都不经过代理。类不需要 virtual，但必须通过接口调用。
- 不支持 async void、ref/out/in 参数、ref 返回、指针/Span 等签名、异步流、自定义 awaitable、Task 派生返回类型、静态接口方法。发现不支持的接口签名会在注册/创建时拒绝；async void 在解析实现时拒绝。
- 服务接口不能继承 IDisposable/IAsyncDisposable，释放方法留在实现类。DI 管理实现类与拦截器的释放，避免代理再次释放真实对象；手动 Create 时由调用者负责其资源生命周期。
- 同一个服务/拦截器实例可能并发调用，钩子本身需线程安全；不要把每次调用的可变状态放在拦截器字段，应存入 context.Items。
- 接口代理依赖运行时动态代码，不支持 NativeAOT；未验证裁剪兼容性。按方法声明的返回类型选择等待方式，object 返回值中装箱的 Task 等不会被当作异步返回。
