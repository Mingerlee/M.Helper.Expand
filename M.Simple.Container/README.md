# M.Simple.Container

.NET 8 依赖注入容器。沿用 `IServiceCollection` 注册和构造函数注入，增加程序集扫描、按接口筛选和一行 Host 接入。

当前版本 `1.0.0.2`，采用四段版本号；新增指定接口映射和代理工厂注册，与 Interception 包互不依赖。性能数据以仓库内基准结果为准。尚不支持 Native AOT。

## ASP.NET Core 接入

```csharp
using M.Simple.Container;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSimpleContainer();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();
app.MapGet("/users/{id}", (int id, IUserService users) => users.Get(id));
app.Run();
```

业务类正常使用构造函数注入；每个 Web 请求有独立作用域，不需要手动解析服务。

## 批量注册：替代 Autofac 程序集扫描

以下写法对应 `RegisterAssemblyTypes(...).AsImplementedInterfaces().InstancePerLifetimeScope()`：

```csharp
builder.Services.AddAssemblyServices(
    Assembly.Load("BAMS.Business"),
    Assembly.Load("BAMS.Repository"));
```

需要 `using System.Reflection;`。只扫描指定程序集，将具体类映射到它实现的接口，默认 `Scoped`。不注册 `IDisposable`、`IAsyncDisposable`。类没有业务接口时不会注册；需要自身类型时使用链式 API 的 `AsSelf()`。

不需要 Autofac 的 `ConfigureContainer<ContainerBuilder>`，原有 `builder.Services.Add...` 继续使用。

## 按指定接口注入代理

Container 与 `M.Simple.Services.Interception` 互不依赖。Container 负责筛选、注册和管理对象生命周期，调用方通过工厂提供代理；代理可以来自自定义实现、DispatchProxy 或其他独立组件。

```csharp
using M.Simple.Container;

builder.Services.Scan(Assembly.Load("MyApp.Business"))
    .AssignableTo<IInterception>() // 项目自行定义的标记接口
    .As<IOrderService>()         // 只暴露指定业务接口，不注册标记接口
    .WithProxy((serviceType, target, provider) =>
        new OrderServiceProxy((IOrderService)target)) // 项目自行实现的代理
    .Scoped();
```

也可使用 `.AsImplementedInterfaces().ExcludeInterface(typeof(IInterception))` 暴露多个业务接口，工厂根据 `serviceType` 创建对应代理。`As<TService>()` / `As(Type)` 可多次调用以指定多个接口。

`WithProxy(factory).Scoped()` 等价于 `ProxyScoped(factory)`，另外支持 `ProxyTransient(factory)`、`ProxySingleton(factory)`，三种方式都需要显式传入工厂。工厂在解析接口时运行，收到业务接口类型、真实对象及当前解析的 IServiceProvider。返回值必须是实现指定接口、且区别于真实对象的非空代理。

同一工厂重复扫描相同映射不增加注册；先普通扫描再代理扫描，会替换选中接口与实现类的普通映射，保留其他实现和命名注册。Scoped/Singleton 的多个代理接口共享同一真实对象；已有真实对象的注册生命周期必须与代理一致。SimpleContainer 中扫描注册的普通接口及 `AsSelf()` 也共享该对象。Transient 每次解析创建新的代理和真实对象。

`AsSelf()` 暴露真实对象，直接调用它不经过代理。真实对象由 DI 释放，代理不应再次释放它；代理自身的可释放资源由 DI 管理。代理接口不能继承 IDisposable/IAsyncDisposable。代理扫描要求闭合类型，不支持开放泛型；注册校验失败时不提交部分扫描结果。工厂内部依赖在解析时由容器校验，Singleton 工厂不得解析 Scoped 服务。

Container 不识别钩子特性或执行前后置方法。Interception 包保留 `AddInterceptedAssemblyServices`、钩子特性、代理创建及单服务注册，不需要依赖 Container。将两个包组合使用时，适配逻辑放在应用项目中。

## 按继承接口筛选

```csharp
public interface IScopedService { }
public interface IUserService : IScopedService { }
public sealed class UserService : IUserService { }

builder.Services.AddAssemblyServices<IScopedService>(typeof(UserService).Assembly);
```

支持间接继承；注册 `IUserService -> UserService`，筛选标记 `IScopedService` 本身不注册。其他未继承标记的接口仍会映射。

精细控制：

```csharp
builder.Services
    .Scan(Assembly.Load("BAMS.Repository"))
    .AssignableTo<IRepository>()
    .Where(type => !type.Name.EndsWith("Legacy"))
    .AsImplementedInterfaces()
    .Scoped();
```

- `AssignableTo<T>()` / `AssignableTo(typeof(IRepository<>))`：筛选实现类，也支持基类。
- `ExcludeInterface(typeof(IMarker))`：排除指定映射接口。
- `AsSelf()`：额外注册自身类型，保留接口映射。
- `.Scoped()` / `.Singleton()` / `.Transient()`：执行扫描并指定生命周期。

扫描类型按完整名称排序；重复扫描相同映射和生命周期不会重复添加。常见开放泛型 `IRepository<T> -> Repository<T>` 支持扫描；重排或嵌套泛型参数的映射会明确报错，请显式注册工厂。程序集无法加载类型时直接抛出加载异常，不静默跳过。

## 生命周期与多接口

- `Singleton`：整个容器共享，工厂和依赖在根作用域中创建。
- `Scoped`：同一作用域共享，不同作用域隔离。
- `Transient`：每次解析创建。

扫描注册的同一实现类，映射多个接口或自身类型时，`Scoped` / `Singleton` 共享实例，并只释放一次。手动独立调用多个 `AddScoped<接口, 实现>()` 时，每个描述符是独立注册，行为与标准 DI 一致。

多个实现保留注册顺序，注入 `IEnumerable<T>` 可获取全部；直接解析单个服务取最后一次注册。精确的闭合泛型注册优先于开放泛型注册。

## 控制台和测试

```csharp
using Microsoft.Extensions.DependencyInjection;
using M.Simple.Container;

var services = new ServiceCollection();
services.AddScoped<IUserService, UserService>();

await using var provider = services.BuildSimpleServiceProvider();
await using var scope = provider.CreateAsyncScope();
var users = scope.ServiceProvider.GetRequiredService<IUserService>();
```

示例需同时注册 `UserService` 构造函数的其他依赖。Web 请求作用域由框架创建。后台任务使用 `IServiceScopeFactory` 创建、释放各自作用域。

容器创建的可释放服务按逆创建顺序清理，支持 `IDisposable` 和 `IAsyncDisposable`，异步释放优先调用 `DisposeAsync()`。只支持异步释放的对象应使用 `await using`；同步释放会报错并继续清理其他同步资源。实例注册 `AddSingleton(existingInstance)` 由调用方负责释放。独立创建的子作用域由创建方负责释放，释放根容器不会替调用方释放仍存活的子作用域。

## 校验、工厂和命名服务

默认开启构建校验和作用域校验：

- 缺失依赖、循环依赖和有歧义的公开构造函数会报错。
- 直接或间接捕获 Scoped 的 Singleton 会报错。
- 从根容器解析 Scoped 会提示创建作用域。
- 工厂内部动态依赖在实际解析时检测，构建校验不执行用户工厂。
- 开放泛型在首次关闭具体类型时验证依赖。

需要调整时显式设置：

```csharp
builder.Host.UseSimpleContainer(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});
```

支持标准 `AddKeyedScoped` / `AddKeyedSingleton` / `AddKeyedTransient`、`[FromKeyedServices]`、`[ServiceKey]` 和 `GetRequiredKeyedService`。支持通过 `KeyedService.AnyKey` 枚举已注册的命名服务；首版不支持以 `AnyKey` 注册通配实现，会在构建阶段明确拒绝。

构建时复制注册集合和选项。之后修改原集合不会影响已有容器。并发解析同一 Singleton / Scoped 只创建一个实例；同一作用域的解析与释放使用互斥保护。用户工厂需要同步返回，不应同步等待依赖于同一作用域的其他线程工作。

## 性能与边界

构造函数计划使用 .NET 8 `ConstructorInvoker`，不再为每个容器执行 `Expression.Compile()`。构造函数及参数元数据通过弱键缓存复用；服务解析委托、命名服务的键、业务实例和生命周期缓存仍按容器隔离。常见的零至四参数构造函数不需要临时参数数组；更多参数使用池化数组，并在返回时清除引用。集合解析也不再编译表达式。

默认构建校验保持开启。注册查找使用索引，避免逐个扫描所有描述符。尚不支持源码生成、Native AOT、属性注入、子容器注册覆盖和运行期间修改注册。不承诺与 Autofac 全功能等价；复杂应用应先验证所用第三方扩展和真实业务依赖图。

基准项目比较 Autofac、微软 DI 与本容器，包括单例、瞬态依赖图、作用域缓存、完整作用域创建/解析/释放及启动成本。短基准适合观察趋势，不能推导生产 API 的吞吐提升。

[性能结果与原始报告](../M.Simple.Container.Benchmarks/README.md) 分别记录原测试版本 `1.0.0` 基线与 `1.0.1` 启动优化后的结果；当前包版本统一为 `1.0.0.1`，相对于已测优化版本仅调整版本号。进程内重复构建和新进程首次构建单独测量。

```powershell
dotnet test M.Simple.Container.Test/M.Simple.Container.Test.csproj -c Release
dotnet run --project M.Simple.Container.Sample -c Release -- --urls http://127.0.0.1:5198
dotnet run --project M.Simple.Container.Benchmarks -c Release -- --filter '*ResolutionBenchmarks*' --job short
dotnet run --project M.Simple.Container.Benchmarks -c Release -- --filter '*StartupBenchmarks*' --job short
dotnet pack M.Simple.Container/M.Simple.Container.csproj -c Release -o artifacts/packages
```

Web 示例：`/users/1` 返回用户信息；`/scope` 返回作用域标识与多接口是否共享实例，不同请求的标识应不同。
