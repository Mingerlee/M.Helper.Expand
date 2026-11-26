using System.Diagnostics;
using M.Simple.Services.Interception;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddInterceptedScoped<IOrderService, OrderService, ConsoleMethodInterceptor>(attributedOnly: true);
// 按方法名特性，不再需要单独的拦截器类。
services.AddInterceptedScoped<INamedOrderService, NamedOrderService>();
services.AddInterceptedScoped<IMessageService, MessageService>();
services.AddScoped<MessageHooks>();
using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
using var scope = provider.CreateScope();
var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
var interceptor = scope.ServiceProvider.GetRequiredService<ConsoleMethodInterceptor>();

Console.WriteLine("1. 方法级拦截：同步方法");
Check(orders.CalculateTotal(2, 15m) == 30m, "同步结果");
Console.WriteLine("2. 方法级拦截：异步创建订单");
Check(await orders.CreateAsync("订单-A") == "订单-A:created", "异步结果");
Console.WriteLine("3. 未标记方法：直接调用，不执行钩子");
orders.Ping();
Console.WriteLine("4. SkipMethodHook：即使标记 MethodHook 也跳过");
orders.Skip();
Console.WriteLine("5. 业务失败：直接抛出，跳过后置");
try { await orders.FailAsync(); throw new Exception("应当失败"); }
catch (InvalidOperationException error) when (error.Message == "模拟订单失败") { }
Console.WriteLine("6. ValueTask：后置获得业务结果");
Check(await orders.QueryAsync(33) == "订单-33", "ValueTask 结果");
Check(interceptor.BeforeCount == 4 && interceptor.AfterCount == 3, "标记与跳过规则");

Console.WriteLine("7. 接口整体拦截：默认注册不需要特性");
var allHooks = new ConsoleMethodInterceptor();
var allMethods = MethodHookProxy<IOrderService>.Create(new OrderService(), allHooks);
allMethods.Ping();
Check(allHooks.BeforeCount == 1 && allHooks.AfterCount == 1, "默认拦截全部方法");
Console.WriteLine("8. 方法名特性：BeforeMethod + AfterMethod，调用同一服务的私有方法");
var named = scope.ServiceProvider.GetRequiredService<INamedOrderService>();
var namedTarget = scope.ServiceProvider.GetRequiredService<NamedOrderService>();
Check(await named.CreateAsync("订单-B") == "订单-B:created", "按名称异步调用");
Check(namedTarget.Events.SequenceEqual(new[] { "before", "business", "after:success" }), "指定方法执行顺序");
namedTarget.Events.Clear();
try { await named.CreateAsync(""); throw new Exception("应当失败"); }
catch (InvalidOperationException error) when (error.Message == "订单名称不能为空") { }
Check(namedTarget.Events.SequenceEqual(new[] { "before", "business" }), "失败时跳过指定后置方法");
Console.WriteLine("9. 前置修改参数，后置修改返回值");
var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
Check(await messageService.SayHolle("你好") == "你好【前置追加】【后置追加】", "参数与结果修改");
Console.WriteLine("全部示例验证通过。");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("示例验证失败：" + message);
}

public interface IOrderService
{
    [MethodHook] decimal CalculateTotal(int quantity, decimal price);
    [MethodHook] Task<string> CreateAsync(string name, CancellationToken cancellationToken = default);
    void Ping();
    [MethodHook, SkipMethodHook] void Skip();
    [MethodHook] Task FailAsync();
    [MethodHook] ValueTask<string> QueryAsync(int id);
}

public interface IMessageService
{
    Task<string> SayHolle(string msg);
}

public sealed class MessageService : IMessageService
{
    [BeforeMethod(typeof(MessageHooks), nameof(MessageHooks.RecordBefore))]
    [AfterMethod(typeof(MessageHooks), nameof(MessageHooks.RecordAfter))]
    public async Task<string> SayHolle(string msg)
    {
        await Task.Yield();
        Console.WriteLine("  [业务收到参数] " + msg);
        return msg;
    }

}

public sealed class MessageHooks
{
    public void RecordBefore(MethodInvocationContext context)
        => context.SetArgument("msg", context.GetArgument<string>("msg") + "【前置追加】");

    public void RecordAfter(MethodInvocationContext context)
    {
        context.SetResult((string)context.Result! + "【后置追加】");
        Console.WriteLine("  [最终返回值] " + context.Result);
    }
}

// An unmarked service remains an ordinary Scoped registration during assembly scanning.
public interface IValueService<T> { T Echo(T value); }
public sealed class ValueService<T> : IValueService<T>
{
    public T Echo(T value) => value;
}

public sealed class OrderService : IOrderService
{
    public decimal CalculateTotal(int quantity, decimal price) => quantity * price;
    public async Task<string> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        Console.WriteLine("  [业务] 创建 " + name);
        return name + ":created";
    }
    public void Ping() => Console.WriteLine("  [业务] Ping");
    public void Skip() => Console.WriteLine("  [业务] Skip");
    public async Task FailAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("模拟订单失败");
    }
    public async ValueTask<string> QueryAsync(int id)
    {
        await Task.Yield();
        return "订单-" + id;
    }
}

public sealed class ConsoleMethodInterceptor : MethodInterceptor
{
    public int BeforeCount { get; private set; }
    public int AfterCount { get; private set; }
    public override void Before(MethodInvocationContext context)
    {
        BeforeCount++;
        context.Items["watch"] = Stopwatch.StartNew();
        Console.WriteLine("  [前置] " + context.Method.Name);
    }
    public override void After(MethodInvocationContext context)
    {
        AfterCount++;
        var watch = (Stopwatch)context.Items["watch"]!;
        Console.WriteLine($"  [后置] {context.Method.Name}，结果={context.Result ?? "无"}，异常={context.Exception?.Message ?? "无"}，耗时={watch.ElapsedMilliseconds}ms");
    }
    // 默认 BeforeAsync / AfterAsync 复用上述同步钩子。
    // 需要异步数据库/网络操作时，改为重写相应异步方法并 await 操作。
}

public interface INamedOrderService
{
    Task<string> CreateAsync(string name);
}

public sealed class NamedOrderService : INamedOrderService
{
    public List<string> Events { get; } = [];

    [BeforeMethod(nameof(BeforeCreate))]
    [AfterMethod(nameof(AfterCreate))]
    public async Task<string> CreateAsync(string name)
    {
        await Task.Yield();
        Events.Add("business");
        Console.WriteLine("  [业务] " + name);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("订单名称不能为空");
        return name + ":created";
    }

    private void BeforeCreate(MethodInvocationContext context)
    {
        Events.Add("before");
        Console.WriteLine("  [指定前置方法] BeforeCreate，参数=" + context.Arguments[0]);
    }

    private async Task AfterCreate(MethodInvocationContext context)
    {
        await Task.Yield();
        Events.Add(context.Exception is null ? "after:success" : "after:failed");
        Console.WriteLine($"  [指定后置方法] AfterCreate，结果={context.Result}，异常={context.Exception?.Message}");
    }
}
