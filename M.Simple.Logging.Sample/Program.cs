using System.Diagnostics;
using M.Simple.Logging;

var builder = WebApplication.CreateBuilder(args);
// 演示站点仅监听回环地址，匿名查看仅适用于本地示例。
builder.WebHost.UseUrls("http://127.0.0.1:5188");
builder.Services.AddSimpleLogging(options =>
{
    options.ApplicationName = "日志包示例";
    options.DatabasePath = "App_Data/sample-logs.db";
});
var app = builder.Build();
app.MapSimpleLogDashboard("/logs").AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/logs"));
app.MapPost("/demo/logs", (ILoggerFactory factory) =>
{
    var logger = factory.CreateLogger("Sample.OrderService");
    using var activity = new Activity("CreateOrder").SetIdFormat(ActivityIdFormat.W3C).Start();
    using var scope = logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = "demo-user" });
    var orderId = Random.Shared.Next(10000, 99999);
    logger.LogInformation("订单 {OrderId} 创建成功", orderId);
    logger.LogWarning("订单 {OrderId} 库存不足，待人工确认", orderId);
    try { throw new InvalidOperationException("示例异常：库存服务暂时不可用。"); }
    catch (Exception ex) { logger.LogError(ex, "订单 {OrderId} 提交失败", orderId); }
    return Results.Ok(new { orderId, traceId = activity.TraceId.ToString() });
});
app.Logger.LogInformation("日志工作台示例已启动，通过 POST /demo/logs 生成演示数据");
app.Run();
