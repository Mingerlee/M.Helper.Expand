# M.Simple 容器设计草案

状态：首版运行时容器已实现，实际入口与限制见 [包说明](../M.Simple.Container/README.md)。本文保留设计依据与后续路线；源码生成和 AOT 尚未实现。

目标：使用简单，在明确的场景中通过实测争取优于 Autofac 的性能；保留 .NET 标准 DI 使用习惯。不能在基准测试前宣称已超越 Autofac。

## 使用体验

新包暂名 `M.Simple.Container`，与现有 `M.Simple.Services.DependencyInjection` 自动注册扩展职责不同。现有扩展基于 `IServiceCollection` 工作，可以作为后续兼容验证对象。首版不改变现有特性的默认生命周期。

ASP.NET Core 项目只需增加一行容器切换代码，服务继续使用标准注册方法：

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSimpleContainer();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();
app.MapGet("/users/{id}", (int id, IUserService service) => service.Get(id));
app.Run();
```

不要求用户学习独立的 ContainerBuilder、注册模块或另一套生命周期名称。业务代码使用构造函数注入，Web 请求由框架管理作用域。

控制台或单元测试使用标准集合和标准解析扩展：

```csharp
var services = new ServiceCollection();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserRepository, UserRepository>();

await using var provider = services.BuildSimpleServiceProvider();
await using var scope = provider.CreateAsyncScope();
var service = scope.ServiceProvider.GetRequiredService<IUserService>();
```

首版推荐显式注册。程序集扫描作为可选便利功能：只扫描调用者指定的程序集；多接口映射必须明确，不按 GetInterfaces() 的首个结果猜测。新增自动注册 API 默认 Scoped；需要无状态、线程安全的单例时显式指定 Singleton。

## 简单的具体标准

- 常规 Web 接入只增加一行，继续使用 AddScoped/AddSingleton/AddTransient。
- 不要求业务类继承容器类型或实现专有接口。
- 不要求业务代码手动获取容器或创建 Web 请求作用域。
- 默认校验可静态发现的缺失依赖、循环依赖和 Singleton 捕获 Scoped；报错给出完整依赖链及修复建议。
- 根容器解析 Scoped 默认报错，提示创建作用域。
- 构建后冻结注册，避免运行期间修改导致缓存语义不明确。
- 工厂内部动态解析无法全部在构建时验证，运行时仍执行生命周期及循环检测。

## 首版实现边界

核心能力包含构造函数注入、三种生命周期、实例与工厂注册、多实现的 IEnumerable<T>、开放泛型、可选参数以及同步和异步释放。构造函数选择规则与标准 DI 行为通过兼容测试确定，遇到歧义给出清晰异常。

标准接口包含 IServiceProvider、IServiceScopeFactory、IServiceProviderIsService，以及 ASP.NET Core 的 IServiceProviderFactory<TBuilder> 适配。还需要验证服务别名、同一实现的多次注册、重复服务取最后一个、IEnumerable<T> 保留注册顺序等行为。

.NET 8 的 keyed service 注册必须在实现前决定支持策略：支持时同时实现对应服务提供接口和兼容测试；未支持时在构建阶段明确拒绝相关描述符，不能静默忽略。ASP.NET Core 可用性以真实启动和请求测试为准。

容器创建的可释放对象由所属容器或作用域持有，按逆创建顺序释放。外部传入的实例归调用方所有。单例依赖的瞬态对象在根作用域创建；同一个实例不能因多条解析路径被重复释放。作用域释放与解析并发的行为必须明确并测试。

首版不纳入属性注入、拦截器、子容器覆盖和运行时注册更新。这些功能后续按实际需求评估，不作为简单接入的前提。

## 性能实现路线

1. 构建阶段分析依赖图、分配缓存槽位，缓存对象创建计划。
2. `1.0.1` 使用 .NET 8 ConstructorInvoker 和弱键元数据缓存，移除逐容器的表达式编译；依赖绑定留在各个容器内，单例与作用域实例使用线程安全缓存。
3. 对多实现和泛型关闭结果缓存计划，并避免无界增长与每次解析额外分配。
4. 第二阶段引入 Source Generator，为编译期已知依赖生成创建代码；动态工厂和运行时注册保留明确的边界。

当前运行时路径不宣称支持 Native AOT。AOT 支持需要源码生成或其他经过验证的路径，并单独执行发布及运行测试。编译阶段能否看到最终注册集合也是源码生成设计问题，不能假定任意 IServiceCollection 调用都能静态分析。

## 验收与比较

正确性先于速度：覆盖生命周期隔离、并发只创建一个单例、嵌套依赖、动态工厂、泛型、多实现、异常链、资源释放和真实 Web 请求作用域。

使用 BenchmarkDotNet 对比 Autofac、微软自带 DI 和等价手写创建代码，固定并记录 .NET、依赖包版本、硬件、构建配置和基准源码。每种实现保持相同注册语义、缓存状态和释放责任。

分别报告构建耗时、首次解析、稳定解析、分配量、作用域创建/释放；覆盖浅层与深层依赖、宽依赖图、三种生命周期、多实现、开放泛型和并发。将释放成本纳入相应完整场景，避免通过漏释放获取虚假的性能收益。

性能结果需要给出多次测量及误差，不能以单个无依赖类型的解析结果宣称全面领先，也不能从容器微基准直接推断 Web API 吞吐量提升。

## 实施顺序

1. 建立核心包、正确性测试、标准 DI 适配和最小可运行示例。
2. 验证 Web 集成与第三方注册兼容，提供最短使用说明。
3. 建立可复现基准，按测量结果优化热点。
4. 独立评估源码生成与 AOT，保持应用侧入口尽量一致。
