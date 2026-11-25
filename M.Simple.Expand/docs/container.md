# M.Simple.Container 接入与使用

`M.Simple.Container` 是面向 .NET 8 的依赖注入容器，沿用 `IServiceCollection` 注册和构造函数注入，提供 Host 接入和程序集批量扫描。本文使用版本 `1.0.0.1`。

## 安装包

在 Web API 项目目录执行，或通过 `--project` 指定项目：

```powershell
dotnet add package M.Simple.Container --version 1.0.0.1
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
- `ExcludeInterface(typeof(IMarker))`：排除指定接口映射。
- `Scoped()`：同一作用域共享实例，适合每个请求的业务服务和仓储。
- `Singleton()`：整个容器共享实例，不能捕获 Scoped 依赖。
- `Transient()`：每次解析创建实例。

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

支持标准命名服务注册、同步及异步资源释放。尚不支持 Native AOT、属性注入、拦截器、子容器注册覆盖或运行期间修改注册。它与 `M.Simple.Services.DependencyInjection` 是不同的包，使用各自对应的命名空间和 API。

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
