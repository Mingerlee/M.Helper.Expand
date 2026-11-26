# M.Simple.Container 接入与使用

`M.Simple.Container` 是面向 .NET 8 的依赖注入容器，沿用 `IServiceCollection` 注册和构造函数注入，提供 Host 接入、程序集批量扫描及按指定接口注入代理。本文使用版本 `1.0.0.4`，与 Interception 包互不依赖。

`1.0.0.3` 新增无参数 `InterceptedScoped()`、`InterceptedTransient()`、`InterceptedSingleton()`。`1.0.0.4` 将 `AddInterceptedAssemblyServices` 扫描入口迁入 Container。下面的普通注册及自定义工厂入口同时兼容 1.0.0.2。

## 安装包

在 Web API 项目目录执行，或通过 `--project` 指定项目：

```powershell
dotnet add package M.Simple.Container --version 1.0.0.4
```

## Program.cs 配置

在现有项目中加入容器接入及程序集扫描，保留已有的 Swagger、统一响应、数据库和认证配置。无需添加示例接口。

```csharp
using M.Simple.Container; // 本次新增：容器接入与程序集扫描扩展。
using System.Reflection; // 本次新增：按名称加载业务程序集。

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSimpleContainer(); // 本次新增：使用 M.Simple.Container 构建应用的服务容器。

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 本次新增：扫描业务层和仓储层，将实现类按接口注册，默认 Scoped。
builder.Services.AddAssemblyServices(
    Assembly.Load("M.Simple.Business"),
    Assembly.Load("M.Simple.Repository"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

这里的程序集名称对应 `M.Simple.Sample.Api` 的业务层和仓储层。其他项目替换为各自的程序集名称；使用 `Assembly.Load()` 时，API 必须引用相应项目或包，使程序集存在于应用输出目录。仅在解决方案中添加项目，不会自动建立项目引用。

也可以使用 `typeof(业务实现类).Assembly` 获取程序集，避免手写程序集名称。业务类继续使用构造函数注入，每个 Web 请求由框架创建独立作用域。

## 接入与扫描的区别

| 调用 | 作用 | 放置位置 |
| --- | --- | --- |
| UseSimpleContainer() | 将 Host 的服务提供程序切换为 M.Simple.Container | builder.Build() 之前 |
| AddAssemblyServices(...) | 扫描指定程序集并批量注册接口和实现类，默认 Scoped | builder.Build() 之前 |

**只安装包不会自动启用容器或扫描业务程序集。** 已有 `AddScoped`、`AddSingleton`、`AddTransient` 等标准注册可以继续使用；如果全部采用显式注册，可省略程序集扫描。

## 程序集扫描规则

- 只扫描传入的程序集，将具体类映射到其实现的接口；没有业务接口的类不会自动注册。
- `IDisposable`、`IAsyncDisposable` 不作为业务服务接口注册。
- 相同映射和生命周期的重复扫描不会重复添加注册。
- 扫描注册的同一实现类映射到多个接口时，Scoped / Singleton 在相应生命周期内共享实例。
- 常见开放泛型映射支持扫描；重排或嵌套泛型参数的映射需显式注册工厂。

## 按接口筛选与指定生命周期

以下代码使用项目已有的接口和实现类型，按实际类型替换：

```csharp
builder.Services.AddAssemblyServices<IScopedService>(typeof(UserService).Assembly);
```

只筛选实现或间接继承标记接口的类；筛选标记接口本身不注册。需要更细控制时使用链式扫描：

```csharp
builder.Services
    .Scan(Assembly.Load("M.Simple.Repository"))
    .AssignableTo<IRepository>()
    .Where(type => !type.Name.EndsWith("Legacy"))
    .AsImplementedInterfaces()
    .Scoped();
```

- `AsSelf()`：额外注册实现类自身类型。
- `As<TService>()` / `As(Type)`：只暴露指定的业务接口，可多次调用以选择多个接口。
- `ExcludeInterface(typeof(IMarker))`：排除指定接口映射。
- `Scoped()`：同一作用域共享实例，适合每个请求的业务服务和仓储。
- `Singleton()`：整个容器共享实例，不能捕获 Scoped 依赖。
- `Transient()`：每次解析创建实例。

## 指定多个程序集

`Scan` 接收多个程序集，也可传入程序集数组：

```csharp
builder.Services.Scan(
        Assembly.Load("M.Simple.Business"),
        Assembly.Load("M.Simple.Repository"))
    .AsImplementedInterfaces()
    .Scoped();
```

也可使用 `typeof(TestLogic).Assembly` 等实际实现类型定位程序集。不能省略程序集；当前不自动扫描整个解决方案或所有应用依赖。

## 无参数拦截扫描（1.0.0.3 起）

应用同时引用 Container 和 Interception 1.0.0.4 或兼容的后续版本后，可直接使用以下链式注册，不需要指定业务接口或手写代理工厂：

```csharp
using System.Reflection;
using M.Simple.Container;

builder.Services.Scan(Assembly.Load("M.Simple.Business"))
    .AssignableTo<IInterception>()
    .ExcludeInterface(typeof(IInterception))
    .AsImplementedInterfaces()
    .InterceptedScoped();
```

`IInterception` 由项目自行定义。实现该标记的业务类会按其业务接口注册代理；标记接口本身排除。业务方法继续使用 Interception 的 `BeforeMethod` / `AfterMethod` 特性；外部钩子类仍需注册到 DI，可在扫描前或后注册。

| 入口 | 生命周期 |
| --- | --- |
| InterceptedScoped() | 每个作用域共享代理和真实对象 |
| InterceptedTransient() | 每次解析创建新的代理和真实对象 |
| InterceptedSingleton() | 全应用共享代理和真实对象，不能捕获 Scoped 依赖 |

Container 在注册阶段按程序集名称加载 Interception 的公开注册方法，之后沿用 Interception 的代理、日志和校验；两个包不增加相互包引用。Interception 不会随 Container 自动安装，应用必须显式引用。缺包或运行时 API 不兼容时，扫描会明确报错；注册失败不提交部分映射。

重复扫描不增加注册；先普通扫描再拦截扫描，会替换相同接口与实现的普通映射。选中的闭合接口都经过代理，按特性执行钩子；开放泛型不支持此入口。同一次扫描不要混用无参数拦截入口与 `WithProxy`。`AddInterceptedAssemblyServices` 由 Container 提供，并与无参数拦截注册共用 Interception 运行时适配。

源码 Web 示例 `M.Simple.Container.Sample` 已采用此方式，访问 `/proxy?message=hello` 可看到前后置处理后的结果及自动生成的代理类型。

## 按指定接口注入代理（自定义工厂，1.0.0.2 起）

Container 管理代理注册和生命周期，调用方提供代理工厂，不要求引用 Interception。以下是完整的类型定义和注册方式：

```csharp
using M.Simple.Container;

// 放在 builder.Build() 之前。
builder.Services.Scan(typeof(GreetingService).Assembly)
    .AssignableTo<IProxyService>()
    .Where(type => type == typeof(GreetingService))
    .As<IGreetingService>()
    .ProxyScoped((_, target, _) => new GreetingServiceProxy((IGreetingService)target));

public interface IProxyService { }
public interface IGreetingService : IProxyService
{
    string SayHello(string message);
}
public sealed class GreetingService : IGreetingService
{
    public string SayHello(string message) => $"{message},hello world!";
}
public sealed class GreetingServiceProxy(IGreetingService target) : IGreetingService
{
    public string SayHello(string message)
        => target.SayHello(message + "【代理前置】") + "【代理后置】";
}
```

注入 `IGreetingService` 后调用 `SayHello("hello")`，结果为 `hello【代理前置】,hello world!【代理后置】`。`IProxyService` 仅筛选实现类，`As<IGreetingService>()` 只暴露业务接口，不注册标记接口。手写代理同样实现业务接口，放在同一程序集时必须通过 `Where` 排除；此例只扫描真实的 `GreetingService`，避免代理类被再次注册为业务实现。

- `WithProxy(factory).Scoped()` 等价于 `ProxyScoped(factory)`，还支持 `ProxyTransient(factory)`、`ProxySingleton(factory)`。
- 工厂参数依次是业务接口类型、真实业务对象、当前解析的 IServiceProvider。返回值必须是实现该接口且区别于真实对象的非空代理。
- 多个业务接口可用多次 `As<TService>()` 指定，或使用 `AsImplementedInterfaces()` 并排除标记接口；工厂按接口类型创建对应代理。
- 同一工厂重复扫描不增加映射。先普通扫描再代理扫描，会替换选中接口与实现类的普通映射，保留其他实现和命名注册。
- Scoped / Singleton 的多个接口代理共享真实对象；已有真实对象注册必须与代理生命周期一致。`AsSelf()` 暴露真实对象，直接调用它不经过代理。
- 真实对象由 DI 释放，代理不应再次释放它。代理接口不能继承 IDisposable/IAsyncDisposable；代理扫描不支持开放泛型。

运行 `dotnet run --project M.Simple.Container.Sample -c Release -- --urls http://127.0.0.1:5198`，访问 `http://127.0.0.1:5198/proxy?message=hello` 可查看代理结果、代理类型和作用域标识。普通服务演示仍可访问 `/users/1` 和 `/scope`。

## 与 Interception 组合使用

使用 Interception 钩子时，优先采用上方 1.0.0.3 新增的无参数拦截扫描，直接调用 `InterceptedScoped()`、`InterceptedTransient()` 或 `InterceptedSingleton()`。应用只需提供钩子特性及方法，不需要手写代理或工厂。

Container 1.0.0.4 可通过自定义工厂组合 Interception 1.0.0.5：

```csharp
using M.Simple.Container;
using M.Simple.Services.Interception;

builder.Services.Scan(typeof(TestLogic).Assembly)
    .As<ITestLogic>()
    .ProxyScoped((_, target, provider) =>
        MethodHookProxy<ITestLogic>.Create((ITestLogic)target, true, provider));
```

`AddInterceptedAssemblyServices` 由 Container 提供，详细用法见本页。无参数拦截扫描、自定义工厂和特性扫描入口选择一种，避免相同映射重复扫描。

## 默认校验与常见问题

默认开启构建校验和作用域校验：缺失依赖、循环依赖、构造函数歧义和 Singleton 捕获 Scoped 会报错。从根容器解析 Scoped 服务时应创建作用域；后台任务使用 `IServiceScopeFactory` 创建并释放作用域。

```csharp
builder.Host.UseSimpleContainer(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});
```

此配置可替换无参数的 `UseSimpleContainer()`，通常保留默认值即可。

1. 找不到业务程序集：检查 API 项目是否引用 Business / Repository 项目，程序集名称是否与输出文件一致。
2. 无法解析业务接口：检查实现类是否在传入的程序集内、是否实现该接口，以及筛选条件是否排除了该类型。
3. 提示缺少构造函数依赖：检查数据库上下文、配置及其他依赖是否均已注册；程序集扫描不会替代数据库等基础服务的配置。
4. 已有 Autofac：使用本容器接入时移除原 Autofac 服务提供程序工厂接入，并将其扫描注册迁移为相应配置，避免多个容器工厂覆盖。

## 适用范围

支持标准命名服务注册、同步及异步资源释放，以及通过工厂注册代理对象；不内置钩子特性解释或 AOP 执行管线。尚不支持 Native AOT、属性注入、子容器注册覆盖或运行期间修改注册。它与 `M.Simple.Services.DependencyInjection` 是不同的包，使用各自对应的命名空间和 API。

## 业务服务注入与调用示例

下面的接口和实现放在项目已有业务程序集中；无需新增控制器：

```csharp
public interface IPriceService
{
    decimal Calculate(decimal price, int quantity);
}

public sealed class PriceService : IPriceService
{
    public decimal Calculate(decimal price, int quantity) => price * quantity;
}

public sealed class CheckoutService(IPriceService prices)
{
    public decimal GetTotal() => prices.Calculate(19.90m, 2);
}
```

Build() 前显式注册，或用前述扫描注册 IPriceService；两种方式选一种，CheckoutService 没有接口时需要注册自身：

```csharp
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<CheckoutService>();
```

现有业务类注入 CheckoutService 后调用 GetTotal()，结果为 39.80。控制台或后台任务可按作用域解析：

```csharp
await using var scope = app.Services.CreateAsyncScope();
var checkout = scope.ServiceProvider.GetRequiredService<CheckoutService>();
decimal total = checkout.GetTotal();
```

app 为已构建的应用，解析扩展需要 Microsoft.Extensions.DependencyInjection。Web 请求内优先构造函数注入，不要从根容器直接获取 Scoped 服务。
